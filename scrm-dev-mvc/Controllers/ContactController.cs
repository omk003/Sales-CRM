using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using scrm_dev_mvc.Models;
using scrm_dev_mvc.Models.ViewModels;
using scrm_dev_mvc.services;
using scrm_dev_mvc.Services;

namespace scrm_dev_mvc.Controllers
{
    [Authorize]
    public class ContactController(IContactService contactService, IUserService userService, IOrganizationService organizationService) : Controller
    {
        public IActionResult Index()
        {
            
            return View();
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

        [HttpPost]
        public async Task<IActionResult> DeleteBulk([FromBody] List<int> ids)
        {
            await contactService.DeleteContactsByIdsAsync(ids);
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

            if (contact.OwnerId == null)
            {
                contact.OwnerId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
            }

            var result = await contactService.UpdateContact(contact); 

            // You can either:
            // 1. Redirect to Index with TempData message
            TempData["SuccessMessage"] = result;
            return RedirectToAction("Index");
        }

    }
}
