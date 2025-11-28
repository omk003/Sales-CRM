using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SCRM_dev.Services;
using scrm_dev_mvc.Data.Repository;
using scrm_dev_mvc.Data.Repository.IRepository;
using scrm_dev_mvc.Models;
using scrm_dev_mvc.Models.DTO;
using scrm_dev_mvc.Models.ViewModels;
using scrm_dev_mvc.services;
using scrm_dev_mvc.services.Interfaces;
using System.Security.Claims;
using System.Web;

namespace scrm_dev_mvc.Controllers
{
    [Authorize]
    public class ContactController(ICallService callService,IContactService contactService, IUserService userService,IEmailService emailService, IOrganizationService organizationService, IGmailService gmailService, IConfiguration configuration, ILogger<ContactController> _logger, ICurrentUserService currentUserService) : Controller
    {
        public IActionResult Index()
        {
            
            return View();
        }

        public IActionResult ContactPreview(int id)
        {
            var userId = currentUserService.GetUserId();
            var contact = contactService.GetContactById(id, userId);
            if(contact == null)
            {
                return NotFound();
            }
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
            var userId = currentUserService.GetUserId();
            var organization = await organizationService.IsInOrganizationById(userId);
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
                contact.OwnerId = currentUserService.GetUserId();
            }
            var result = await contactService.CreateContactAsync(contact);

            TempData["Message"] = result;
            return RedirectToAction("Index");
        }

        
        //[HttpGet]
        //public async Task<IActionResult> GetAll()
        //{
        //    var userId = currentUserService.GetUserId();

        //    if (userId == Guid.Empty)
        //        return Unauthorized();
            
        //    List<ContactResponseViewModel> ContactList = await contactService.GetAllContacts(userId);
        //    return Json(new { data = ContactList });
        //}


        [HttpPost] 
        public async Task<IActionResult> GetAll([FromBody] DataTableRequest request)
        {
            
            var userId = currentUserService.GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var skip = request.Start;
            var take = request.Length;
            var searchValue = request.Search?.Value;

            var result = await contactService.GetContactsPagedAsync(userId, skip, take, searchValue);

            return Json(new
            {
                draw = request.Draw,
                recordsTotal = result.TotalItems,
                recordsFiltered = result.FilteredItems,
                data = result.Data
            });
        }


        [HttpGet]
        public async Task<IActionResult> GetAllContactsForCompany(int? companyId)
        {
            var userId = currentUserService.GetUserId();

            if (userId == Guid.Empty)
                return Unauthorized();

            List<ContactResponseViewModel> ContactList = await contactService.GetAllContactsForCompany(userId, companyId);
            return Json(new { data = ContactList });
        }


        [HttpPost]
        public async Task<IActionResult> DeleteBulk([FromBody] List<int> ids)
        {
            var userId = currentUserService.GetUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized();
            }
            await contactService.DeleteContactsByIdsAsync(ids, userId);
            return Json(new { success = true, message = "Delete Successful" });
        }

        
        public async Task<IActionResult> Update(int id)
        {
            var userId = currentUserService.GetUserId();
            var organization = await organizationService.IsInOrganizationById(userId);

            var contactEntity = contactService.GetContactById(id, userId);
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
                var vm = new ContactFormViewModel
                {
                    Contact = contact,
                    LeadStatuses = (await contactService.GetLeadStatusesAsync()).ToList(),
                    Lifecycle = (await contactService.GetLifeCycleStagesAsync()).ToList(),
                    Users = (await userService.GetAllUsersAsync()).ToList()
                };
                return View(vm);
            }
            var userId = currentUserService.GetUserId();
            if (contact.OwnerId == null)
            {
                contact.OwnerId = userId;
            }

            var result = await contactService.UpdateContact(contact, userId); 

            
            TempData["Message"] = result;
            return RedirectToAction("Index");
        }


        [HttpPost]
        public async Task<IActionResult> SendEmail(string contactEmail, string subject, string body)
        {
            var userId = currentUserService.GetUserId();
            if (userId == Guid.Empty)
            {
                return StatusCode(401, new { message = "User is not authenticated." });
            }

            string redirectUri = Url.Action("YourGoogleCallbackMethod", "Auth", null, Request.Scheme);

            var result = await emailService.SendEmailAsync(
                userId,
                contactEmail, 
                subject,
                body,
                redirectUri
            );

            if (result.IsSuccess)
            {
                return Ok(new { message = "Email sent and activity logged.", data = result.SentMessage });
            }

            if (result.IsNotFound)
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

            return StatusCode(500, new { message = result.ErrorMessage ?? "An unknown error occurred." });
        }


        [HttpPost]
        public async Task<IActionResult> CallContact(string phoneNumber, int contactId)
        {
            var userId = currentUserService.GetUserId();
            if (userId == Guid.Empty)
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
            var userId = currentUserService.GetUserId();

            var result = await contactService.AssociateContactToCompany(contactId, companyId, userId);

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
        public async Task<IActionResult> AssociateContactToDeal(int contactId, int dealId)
        {
            if (contactId <= 0 || dealId <= 0)
            {
                return BadRequest(new { success = false, message = "Invalid IDs provided." });
            }
            var userId = currentUserService.GetUserId();

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
            var userId = currentUserService.GetUserId();

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
            var userId = currentUserService.GetUserId();

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
