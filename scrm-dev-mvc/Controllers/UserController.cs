using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using scrm_dev_mvc.Models;
using scrm_dev_mvc.Models.ViewModels;
using scrm_dev_mvc.services;
using scrm_dev_mvc.services.Interfaces;
using scrm_dev_mvc.Services;
using System.Security.Claims;
using System.Threading.Tasks;

namespace scrm_dev_mvc.Controllers
{
    public class UserController : Controller
    {
        private readonly IUserService _userService;
        private readonly IOrganizationService _organizationService;
        private readonly ICurrentUserService _currentUserService;

        public UserController(IUserService userService, IOrganizationService organizationService, ICurrentUserService currentUserService)
        {
            _userService = userService;
            _organizationService = organizationService;
            _currentUserService = currentUserService;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _currentUserService.GetUserId();
            if (userId == Guid.Empty)
            {
                return RedirectToAction("Login", "Auth");
            }
            var user = await _userService.GetFirstOrDefault(u => u.Id == userId , "Role");
            var organization = await _organizationService.IsInOrganizationById(userId);

            if (user == null)
            {
                return NotFound("User not found");
            }

            var model = new UserViewModel
            {
                Id = user.Id,
                FirstName = user.FirstName ?? "",
                LastName = user.LastName ?? "",
                Email = user.Email,
                OrganizationName = organization?.Name ?? "N/A",
                Role = user.Role?.Name,
                IsSyncedWithGoogle = user.IsSyncedWithGoogle,
            };

            return View(model);
        }

        public async Task<IActionResult> ProfileUpdate()
        {
            var userId = _currentUserService.GetUserId();
            var user = await _userService.GetFirstOrDefault(u => u.Id == userId, "Role,Organization");
            var model = new UserViewModel
            {
                Id = user.Id,
                FirstName = user.FirstName ?? "",
                LastName = user.LastName ?? "",
                Email = user.Email,
                OrganizationName = user?.Organization.Name ?? "N/A",
                Role = user.Role?.Name,
                IsSyncedWithGoogle = user.IsSyncedWithGoogle,
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(UserViewModel user)
        {
            if (!ModelState.IsValid)
            {
                return View("Index", user);
            }

            var existingUser = await _userService.GetUserByIdAsync(user.Id);
            if (existingUser == null)
            {
                TempData["Message"] = "User not found.";
                return RedirectToAction("Index", "Home");
            }

            existingUser.FirstName = user.FirstName;
            existingUser.LastName = user.LastName;
            var ownerId = _currentUserService.GetUserId();
            await _userService.UpdateUserProfileAsync(user.Id, user.FirstName, user.LastName, ownerId);

            TempData["Message"] = "Profile updated successfully!";
            return RedirectToAction("Index"); 
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SalesAdminSuper,SalesAdmin")]
        public async Task<IActionResult> ChangeUserRole(int organizationId, Guid userId, string newRole)
        {
            var adminId = _currentUserService.GetUserId();
            var success = await _userService.ChangeUserRoleAsync(userId, organizationId, newRole, adminId);
            if (!success)
            {
                TempData["Message"] = "Failed to update user role.";
                return RedirectToAction("OrganizationView", "Organization",new { id = organizationId });
            }
            TempData["Message"] = "User role updated successfully.";
            return RedirectToAction("OrganizationView", "Organization", new { id = organizationId });
        }

        
    }
}
