using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SCRM_dev.Services;
using scrm_dev_mvc.Data.Repository;
using scrm_dev_mvc.Data.Repository.IRepository;
using scrm_dev_mvc.Models;
using scrm_dev_mvc.Models.ViewModels;
using scrm_dev_mvc.services;
using scrm_dev_mvc.services.Interfaces;
using System.Security.Claims;
using System.Web;

namespace scrm_dev_mvc.Controllers
{
    [Authorize]
    public class ContactController(ICallService callService,IContactService contactService, IUserService userService,IEmailService emailService, IOrganizationService organizationService, IGmailService gmailService, IConfiguration configuration, ILogger<ContactController> _logger) : Controller
    {
        public IActionResult Index()
        {
            
            return View();
        }

        public IActionResult ContactPreview(int id)
        {
            var contact = contactService.GetContactById(id);
            var ContactPreview = new ContactPreviewViewModel
            {
                Id = id,
                Name = contact.FirstName ?? "",
                Email = contact.Email,
                PhoneNumber = contact.Number ?? "",
                JobTitle = contact.JobTitle ?? "",

                CreatedAt = contact.CreatedAt,

                LastActivityDate = DateTime.Now,

                LifecycleStage = contact.LifeCycleStage?.LifeCycleStageName ?? "",

                LeadStatus = contact.LeadStatus?.LeadStatusName ?? "",

                Company = contact.Company ?? new Company(),

                Deals = (contact.Deals).ToList(),

                Activities = contact.Activities.ToList()
            };

            return View(ContactPreview);
        }
        public async Task<IActionResult> Insert()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var organization = await organizationService.IsInOrganizationById(Guid.Parse(userId));
            var leadStatuses = await contactService.GetLeadStatusesAsync();
            var lifeCycleStages = await contactService.GetLifeCycleStagesAsync();
            var userIds = await userService.GetAllUsersByOrganizationIdAsync(organization.Id);

            var viewModel = new ContactFormViewModel
            {
                Contact = new ContactDto(),
                LeadStatuses = leadStatuses.ToList(),
                Lifecycle = lifeCycleStages.ToList(),
                Users = userIds
            };

            return View(viewModel);
        }


        [HttpPost]
        public async Task<IActionResult> Insert(ContactDto contact)
        {
            if (!ModelState.IsValid)
            {
                // re-populate dropdowns if validation fails
                var vm = new ContactFormViewModel
                {
                    Contact = contact,
                    LeadStatuses = (await contactService.GetLeadStatusesAsync()).ToList(),
                    Lifecycle = (await contactService.GetLifeCycleStagesAsync()).ToList()
                };
                return View(vm);
            }
            if(contact.OwnerId == null)
            {
                contact.OwnerId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
            }
            var result = await contactService.CreateContactAsync(contact);

            // Store message for next request
            TempData["Message"] = result;
            return RedirectToAction("Index");
        }

        
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            Guid id = Guid.Parse(userId);

            
            List<ContactResponseViewModel> ContactList = await contactService.GetAllContacts(id);
            return Json(new { data = ContactList });
        }

        [HttpGet]
        public async Task<IActionResult> GetAllContactsForCompany(int? companyId)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            Guid id = Guid.Parse(userId);


            List<ContactResponseViewModel> ContactList = await contactService.GetAllContactsForCompany(id, companyId);
            return Json(new { data = ContactList });
        }


        [HttpPost]
        public async Task<IActionResult> DeleteBulk([FromBody] List<int> ids)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }
            await contactService.DeleteContactsByIdsAsync(ids, Guid.Parse(userId));
            return Json(new { success = true, message = "Delete Successful" });
        }

        
        public async Task<IActionResult> Update(int id)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var organization = await organizationService.IsInOrganizationById(Guid.Parse(userId));

            var contactEntity = contactService.GetContactById(id);
            if (contactEntity == null) return NotFound();

            var leadStatuses = await contactService.GetLeadStatusesAsync();
            var lifeCycleStages = await contactService.GetLifeCycleStagesAsync();
            var users = await userService.GetAllUsersByOrganizationIdAsync(organization.Id);
            
            var viewModel = new ContactFormViewModel
            {
                Contact = new ContactDto
                {
                    Id = contactEntity.Id,
                    FirstName = contactEntity.FirstName,
                    LastName = contactEntity.LastName,
                    Email = contactEntity.Email,
                    Number = contactEntity.Number,
                    JobTitle = contactEntity.JobTitle,
                    LeadStatusId = contactEntity.LeadStatusId,
                    LifeCycleStageId = contactEntity.LifeCycleStageId,
                    OwnerId = contactEntity.OwnerId
                },
                LeadStatuses = leadStatuses.ToList(),
                Lifecycle = lifeCycleStages.ToList(),
                Users = users
            };

            return View(viewModel);
        }

        
        [HttpPost]
        public async Task<IActionResult> Update(ContactDto contact)
        {
            if (!ModelState.IsValid)
            {
                // If invalid, reload dropdowns and return form
                var vm = new ContactFormViewModel
                {
                    Contact = contact,
                    LeadStatuses = (await contactService.GetLeadStatusesAsync()).ToList(),
                    Lifecycle = (await contactService.GetLifeCycleStagesAsync()).ToList(),
                    Users = (await userService.GetAllUsersAsync()).ToList()
                };
                return View(vm);
            }
            var userId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
            if (contact.OwnerId == null)
            {
                contact.OwnerId = userId;
            }

            var result = await contactService.UpdateContact(contact, userId); 

            // You can either:
            // 1. Redirect to Index with TempData message
            TempData["Message"] = result;
            return RedirectToAction("Index");
        }


        [HttpPost]
        public async Task<IActionResult> SendEmail(string contactEmail, string subject, string body)
        {
            // 1. Get User ID
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdStr, out Guid currentUserId))
            {
                return StatusCode(401, new { message = "User is not authenticated." });
            }

            // 2. Get Redirect URI
            string redirectUri = Url.Action("YourGoogleCallbackMethod", "Auth", null, Request.Scheme);
            // --- IMPORTANT: Replace with your actual callback URL/method ---

            // 3. Call the service (passing contactEmail, not contact.Id)
            var result = await emailService.SendEmailAsync(
                currentUserId,
                contactEmail, // <-- Pass the email string directly
                subject,
                body,
                redirectUri
            );

            // 4. Handle the detailed result
            if (result.IsSuccess)
            {
                return Ok(new { message = "Email sent and activity logged.", data = result.SentMessage });
            }

            if (result.IsNotFound) // <-- Handle the new property
            {
                return NotFound(new { message = result.ErrorMessage });
            }

            if (result.AuthenticationRequired)
            {
                return StatusCode(401, new
                {
                    message = "Authentication required.",
                    authenticationUrl = result.AuthenticationUrl
                });
            }

            // Catch-all for other failures
            return StatusCode(500, new { message = result.ErrorMessage ?? "An unknown error occurred." });
        }


        [HttpPost]
        public async Task<IActionResult> CallContact(string phoneNumber, int contactId)
        {
            // 2. Get the current user's ID
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out Guid userId))
            {
                _logger.LogWarning("CallContact: Could not find valid User ID in claims.");
                return Unauthorized("User is not authenticated.");
            }

            if (string.IsNullOrEmpty(phoneNumber) || contactId <= 0)
            {
                return BadRequest("Phone number and contact ID are required.");
            }

            try
            {
                var decodedNumber = HttpUtility.HtmlDecode(phoneNumber);

                // 3. Call the updated service method with all required parameters
                var sid = await callService.MakeCallAsync(decodedNumber, userId, contactId);

                _logger.LogInformation("CallContact successful for User: {UserId}, Contact: {ContactId}, SID: {CallSid}", userId, contactId, sid);
                return Ok(new { callSid = sid });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CallContact failed for User: {UserId}, Contact: {ContactId}, Phone: {PhoneNumber}", userId, contactId, phoneNumber);
                return StatusCode(500, "An error occurred while making the call.");
            }
        }


        #region Association Methods

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssociateContactToCompany(int contactId, int companyId)
        {
            var userId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);

            var result = await contactService.AssociateContactToCompany(contactId, companyId, userId);

            if (result.Success)
            {
                // Send a success response
                return Ok(new { message = result.Message });
            }
            else
            {
                // Send a 400 Bad Request response with the error message
                return BadRequest(new { message = result.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> AssociateContactToDeal(int contactId, int dealId)
        {
            if (contactId <= 0 || dealId <= 0)
            {
                return BadRequest(new { success = false, message = "Invalid IDs provided." });
            }
            var userId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);

            var result = await contactService.AssociateContactToDealAsync(contactId, dealId, userId);

            if (result.Success)
            {
                return Ok(new { success = true, message = "Contact associated with deal successfully." });
            }
            else
            {
                return StatusCode(500, new { success = false, message = "An error occurred while associating the contact with the deal." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DisassociateCompany(int contactId)
        {
            var userId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);

            var result = await contactService.DisassociateCompany(contactId, userId);

            if (result.Success)
            {
                return Ok(new { message = result.Message });
            }
            else
            {
                return BadRequest(new { message = result.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DisassociateContactFromDeal(int contactId, int dealId)
        {
            var userId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);

            var result = await contactService.DisassociateContactFromDealAsync(contactId, dealId, userId);

            if (result.Success)
            {
                return Ok(new { message = result.Message });
            }
            else
            {
                return BadRequest(new { message = result.Message });
            }
        }


        #endregion
    }
}
