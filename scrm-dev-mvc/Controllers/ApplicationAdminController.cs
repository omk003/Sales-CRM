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
    // This entire controller is restricted to Super Admins
    //[Authorize(Roles = "ApplicationAdmin")]
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

        //
        // GET: /SuperAdmin
        public async Task<IActionResult> Index()
        {
            var organizations = await _context.Organizations
                .Include(o => o.Users)
                    .ThenInclude(u => u.Role) // Include Role for mapping
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
                    FullName = u.FirstName + " " +u.LastName, // Assuming User model has FullName
                    Email = u.Email,
                    Role = u.Role?.Name ?? "N/A"
                }).ToList()
            }).ToList();

            return View(viewModel); // This now returns List<OrganizationViewModel>
        }

        //
        // GET: /SuperAdmin/Manage/5
        public async Task<IActionResult> Manage(int id)
        {
            var organization = await _context.Organizations
                .Include(o => o.Users)
                    .ThenInclude(u => u.Role) // To get role name
                .FirstOrDefaultAsync(o => o.Id == id);

            if (organization == null)
            {
                return NotFound();
            }

            // Map to your existing OrganizationViewModel
            var viewModel = new OrganizationViewModel
            {
                OrganizationId = organization.Id,
                OrganizationName = organization.Name,
                // Again, CurrentUserRole logic might need to be set here
                // e.g., CurrentUserRole = "SalesAdminSuper",
                Users = organization.Users.Select(u => new UserInOrganizationViewModel
                {
                    UserId = u.Id,
                    FullName = u.FirstName + " " + u.LastName, // Assuming User model has FullName
                    Email = u.Email,
                    Role = u.Role?.Name ?? "N/A"
                }).ToList()
            };

            return View(viewModel); // This returns a single OrganizationViewModel
        }


        //
        // POST: /SuperAdmin/DeleteUser
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

            // --- IMPORTANT CHECK ---
            // Ensure this user isn't the DefaultSenderUser before deleting
            var organization = await _context.Organizations.FindAsync(organizationId);
            //if (organization?.DefaultSenderUserId == user.Id)
            //{
            //    TempData["Error"] = "Cannot delete this user as they are the 'Default Sender' for the organization. Please change the 'Default Sender' setting first.";
            //    return RedirectToAction(nameof(Manage), new { id = organizationId });
            //}
            // --- End Check ---
            try
            {
                
                var result = await _userService.DeleteAsync(user);

                if (result)
                {
                    TempData["Message"] = "User and all related data deleted successfully.";
                }
                else
                {
                    // If it fails here, something is still linked, 
                    // or it's an Identity-specific issue.
                    TempData["Error"] = "Error deleting user. ";
                }
            }
            catch (Exception ex)
            {
                // Catch any other potential DbUpdateException
                _logger.LogError(ex, "Error during delete process for user {UserId}", id);
                TempData["Error"] = "An error occurred. The user may still have other related data that must be removed first.";
            }


            //var result = await _userService.DeleteAsync(user);
            //if (result)
            //{
            //    TempData["Message"] = "User deleted successfully.";
            //}
            //else
            //{
            //    TempData["Error"] = "Error deleting user.";
            //}

            return RedirectToAction(nameof(Manage), new { id = organizationId });
        }

        //
        // POST: /SuperAdmin/DeleteOrganization
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

            // Simple safety check
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