using AspNetCoreGeneratedDocument;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using scrm_dev_mvc.Data;
using scrm_dev_mvc.DataAccess.Data;
using scrm_dev_mvc.Models; 
using scrm_dev_mvc.Models.ViewModels;
using scrm_dev_mvc.services;
using scrm_dev_mvc.services.Interfaces;


namespace scrm_dev_mvc.Controllers
{
    [Authorize(Roles = "ApplicationAdmin")]
    public class ApplicationAdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ApplicationAdminController> _logger;
        private readonly IUserService _userService;

        public ApplicationAdminController(ApplicationDbContext context,
                                   ILogger<ApplicationAdminController> logger, IUserService userService)
                                   
        {
            _context = context;
            _logger = logger;
            _userService = userService;

        }

        
        public async Task<IActionResult> Index()
        {
            var organizations = await _context.Organizations
                .Include(o => o.Users)
                    .ThenInclude(u => u.Role) 
                .ToListAsync();

            // Map to your existing ViewModel
            var viewModel = organizations.Select(o => new OrganizationViewModel
            {
                OrganizationId = o.Id,
                OrganizationName = o.Name,
                // Note: CurrentUserRole is not set here, as we don't know the *current* user's role
                // relative to this specific organization in this context. 
                // We just know they are a SuperAdmin. You may need to adjust this logic
                // if CurrentUserRole is critical for the Index view.
                Users = o.Users.Select(u => new UserInOrganizationViewModel
                {
                    UserId = u.Id,
                    FullName = u.FirstName + " " +u.LastName, 
                    Email = u.Email,
                    Role = u.Role?.Name ?? "N/A"
                }).ToList()
            }).ToList();

            return View(viewModel); 
        }

       
        public async Task<IActionResult> Manage(int id)
        {
            var organization = await _context.Organizations
                .Include(o => o.Users)
                    .ThenInclude(u => u.Role) 
                .FirstOrDefaultAsync(o => o.Id == id);

            if (organization == null)
            {
                return NotFound();
            }

            var viewModel = new OrganizationViewModel
            {
                OrganizationId = organization.Id,
                OrganizationName = organization.Name,
                
                Users = organization.Users.Select(u => new UserInOrganizationViewModel
                {
                    UserId = u.Id,
                    FullName = u.FirstName + " " + u.LastName, 
                    Email = u.Email,
                    Role = u.Role?.Name ?? "N/A"
                }).ToList()
            };

            return View(viewModel); 
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(Guid id, int organizationId)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction(nameof(Manage), new { id = organizationId });
            }

            
            var organization = await _context.Organizations.FindAsync(organizationId);
            
            try
            {
                
                var result = await _userService.DeleteAsync(user);

                if (result)
                {
                    TempData["Message"] = "User and all related data deleted successfully.";
                }
                else
                {
                    
                    TempData["Error"] = "Error deleting user. ";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during delete process for user {UserId}", id);
                TempData["Error"] = "An error occurred. The user may still have other related data that must be removed first.";
            }

            return RedirectToAction(nameof(Manage), new { id = organizationId });
        }

       
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteOrganization(int id)
        {
            var organization = await _context.Organizations
                .Include(o => o.Users)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (organization == null)
            {
                return NotFound();
            }

            if (organization.Users.Any())
            {
                TempData["Error"] = $"Cannot delete {organization.Name} because it still has active users. Please remove users first.";
                return RedirectToAction(nameof(Manage), new { id = id });
            }

            try
            {
                _context.Organizations.Remove(organization);
                await _context.SaveChangesAsync();
                TempData["Message"] = $"Organization '{organization.Name}' has been permanently deleted.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting organization {OrgId}", id);
                TempData["Error"] = "An error occurred. The organization may have related data (like deals or contacts) that must be removed first.";
                return RedirectToAction(nameof(Manage), new { id = id });
            }
        }
    }
}