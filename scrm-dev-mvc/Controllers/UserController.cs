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

        public UserController(IUserService userService, IOrganizationService organizationService)
        {
            _userService = userService;
            _organizationService = organizationService;
        }

        public async Task<IActionResult> Index()
        {
            //Get the current logged-in user's ID from claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                // User not logged in, redirect to login
                return RedirectToAction("Login", "Auth");
            }

            var userId = Guid.Parse(userIdClaim.Value);

            // 2️⃣ Get user from the database
            var user = await _userService.GetFirstOrDefault(u => u.Id == userId , "Role");
            var organization = await _organizationService.IsInOrganizationById(userId);

            if (user == null)
            {
                return NotFound("User not found");
            }

            // 3️⃣ Map user data to ViewModel
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

            // 4️⃣ Pass to the Profile view
            return View(model);
        }

        public async Task<IActionResult> ProfileUpdate()
        {
            var userId = User.GetUserId();
            var user = await _userService.GetFirstOrDefault(u => u.Id == userId, "Role,Organization");
            //var organization = await _organizationService.IsInOrganizationById(userId);
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
                // Return the same view with validation errors
                return View("Index", user);
            }

            var existingUser = await _userService.GetUserByIdAsync(user.Id);
            if (existingUser == null)
            {
                TempData["Message"] = "User not found.";
                return RedirectToAction("Index", "Home");
            }

            // Update only editable fields
            existingUser.FirstName = user.FirstName;
            existingUser.LastName = user.LastName;
            var ownerId = User.GetUserId();
            await _userService.UpdateUserProfileAsync(user.Id, user.FirstName, user.LastName, ownerId);

            TempData["Message"] = "Profile updated successfully!";
            return RedirectToAction("Index"); // Redirect to refresh the data
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeUserRole(int organizationId, Guid userId, string newRole)
        {
            // You may want to check if the current user has permission to change roles here.
            var adminId = User.GetUserId();
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
