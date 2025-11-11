using Microsoft.AspNetCore.Mvc;
using SCRM_dev.Services;
using scrm_dev_mvc.Models.ViewModels;
using scrm_dev_mvc.services;
using scrm_dev_mvc.services.Interfaces;

namespace scrm_dev_mvc.Controllers
{
    public class CompanyController(IOrganizationService organizationService, IUserService userService, ICompanyService companyService, ILogger<CompanyController> _logger) : Controller
    {
        public IActionResult Index()
        {
            return View();
        }


        public async Task<IActionResult> Insert()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var organization = await organizationService.IsInOrganizationById(Guid.Parse(userId));
            var userIds = await userService.GetAllUsersByOrganizationIdAsync(organization.Id);

            var viewModel = new CompanyFormViewModel
            {
                Company = new CompanyViewModel(),
                Users = userIds
            };

            return View(viewModel);
        }


        [HttpPost]
        public async Task<IActionResult> Insert(CompanyViewModel company)
        {
            if (!ModelState.IsValid)
            {
                
                return View(company);
            }
            if (company.userId == null)
            {
                company.userId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
            }
            var result = await companyService.CreateCompanyAsync(company);

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
            List<CompanyViewModel> CompanyList = await companyService.GetAllCompany(id);
            return Json(new { data = CompanyList });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteBulk([FromBody] List<int> ids)
        {
            await companyService.DeleteCompanyByIdsAsync(ids);
            return Json(new { success = true, message = "Delete Successful" });
        }


        public async Task<IActionResult> Update(int id)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var organization = await organizationService.IsInOrganizationById(Guid.Parse(userId));

            var companyEntity = companyService.GetCompanyById(id);
            if (companyEntity == null) return NotFound();

            var users = await userService.GetAllUsersByOrganizationIdAsync(organization.Id);

            var viewModel = new CompanyFormViewModel
            {
                Company = new CompanyViewModel
                {
                    Id = companyEntity.Id,
                    Name = companyEntity.Name,
                    City = companyEntity.City,
                    Country = companyEntity.Country,
                    userId = companyEntity.UserId ?? Guid.Empty,
                    Domain = companyEntity.Domain,
                    CreatedDate = companyEntity.CreatedAt,
                },
                Users = users
            };

            return View(viewModel);
        }


        [HttpPost]
        public async Task<IActionResult> Update(CompanyViewModel company)
        {
            if (!ModelState.IsValid)
            {
                // If invalid, reload dropdowns and return form
                var vm = new CompanyFormViewModel
                {
                    Company = company,
                    Users = (await userService.GetAllUsersAsync()).ToList()
                };
                
                return View(vm);
            }

            if (company.userId == null)
            {
                company.userId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
            }

            var result = await companyService.UpdateCompany(company);

            
            // 1. Redirect to Index with TempData message
            TempData["SuccessMessage"] = result;
            return RedirectToAction("Index");

        }


        public async Task<IActionResult> CompanyPreview(int id)
        {
            var company = await companyService.GetCompanyForPreviewAsync(id);

            if (company == null)
            {
                _logger.LogWarning("CompanyPreview: Company with ID {CompanyId} not found.", id);
                return NotFound();
            }

            // --- THIS COMPLETES THE TODO ---
            // 2. Flatten the activities from all contacts into one list
            var allActivities = company.Contacts?
                .SelectMany(c => c.Activities) // Get all activities from all contacts
                .OrderByDescending(a => a.ActivityDate) // Order them
                .ToList() ?? new List<scrm_dev_mvc.Models.Activity>();
            // --- END TODO ---

            // 3. Create the ViewModel
            var model = new CompanyPreviewViewModel()
            {
                Id = id,
                Name = company.Name,
                Domain = company.Domain,
                Contacts = company.Contacts,
                Deals = company.Deals,
                CreatedAt = company.CreatedAt,
                City = company.City,
                Country = company.Country,
                Activities = allActivities // Assign the new flattened list
            };

            return View(model);
        }
    }
}
