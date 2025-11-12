using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using scrm_dev_mvc.Models;
using scrm_dev_mvc.Models.DTO;
using scrm_dev_mvc.Models.ViewModels;
using scrm_dev_mvc.services;
using scrm_dev_mvc.services.Interfaces;
using System.Security.Claims;

namespace scrm_dev_mvc.Controllers
{
    [Authorize]
    public class OrganizationController(IOrganizationService organizationService,IConfiguration configuration, IInvitationService invitationService, IGmailService gmailService, ICurrentUserService currentUserService) : Controller
    {
        public IActionResult Index()
        {
            
            return View();
        }

        public IActionResult Register()
        {
            var userId = currentUserService.GetUserId();

            // check if he is in organization
            if (!string.IsNullOrEmpty(organizationService.IsInOrganizationById(userId).Result?.Name))
            {
                //TempData["Error"] = "You are already in an organization.";
                return RedirectToAction("Index", "Organization");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(Organization organization)
        {
            
            var userId = currentUserService.GetUserId();

            // Pass User in Service layer of organization
            await organizationService.CreateOrganizationAsync(organization, userId);

            // TODO: FIX
            if (string.IsNullOrEmpty(organizationService.IsInOrganizationById(userId).Result?.Name))
            {
                var org = organizationService.IsInOrganizationById(userId).Result;
                var identity = (ClaimsIdentity)User.Identity;

                // Check if the claim already exists
                if (!identity.HasClaim(c => c.Type == "OrganizationName"))
                {
                    identity.AddClaim(new Claim("OrganizationName", org.Name));
                }

                // Re-sign in with updated claims
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(identity),
                    new AuthenticationProperties { IsPersistent = true }
                );
            }
            


            return RedirectToAction("Index","Organization");
        }

        [HttpPost]
        [Authorize(Roles ="SalesAdminSuper,SalesAdmin")]
        public async Task<IActionResult> SendInvitation(string email, int roleId)
        {
            var userId = currentUserService.GetUserId();
            
            // Get the organization of the user sending the invitation
            var organization = await organizationService.IsInOrganizationById(userId);
            if (organization == null)
            {
                TempData["Error"] = "You must be in an organization to send invitations.";
                return RedirectToAction("Index");
            }

            // 1. Create the invitation in the database
            var invitation = await invitationService.CreateInvitationAsync(email, organization.Id, roleId, userId);

            if (invitation == null)
            {
                // --- THIS BLOCK IS FIXED ---
                // An active invitation already exists.
                // Use TempData to send the error message back to the Index view.
                TempData["Message"] = "An active invitation has already been sent to this email address.";
                return RedirectToAction("Index"); // Redirect back to the page with the form
            }

            string? adminId = configuration["Gmail:AdminEmailId"];
            // 2. Send the invitation via email
            await gmailService.SendEmailAsync(Guid.Parse(adminId ?? ""),email,"Invitation from SCRM",$"Hey, you just got invited to SCRM in {organization.Name}, log in to the SCRM using this link - {"https://maudlinly-nonreactive-arturo.ngrok-free.dev/auth/login?invitationcode=" + invitation.InvitationCode}   ,            if new user use this link {"https://maudlinly-nonreactive-arturo.ngrok-free.dev/auth/register?invitationcode=" + invitation.InvitationCode}","");

            TempData["Message"] = $"Invitation sent successfully to {email}!";
            return RedirectToAction("Index");
        }

        
        public async Task<IActionResult> OrganizationView()
        {
            var userId = currentUserService.GetUserId();
            if (userId == Guid.Empty) {
                return Unauthorized();
            }
            var organizationViewModel = await organizationService.GetOrganizationViewModelByUserId(userId);
            return View(organizationViewModel);
        }

        [Authorize(Roles ="ApplicationAdmin")]
        public async Task<IActionResult> OrganizationViewAdmin()
        {
            var organizations = await organizationService.GetAllOrganizationsAsync();
            return View(organizations);
        }


        // GET: Organization/UpdateDetails/{organizationId}
        [Authorize(Roles = "SalesAdminSuper")]
        [HttpGet]
        public async Task<IActionResult> UpdateDetails(int organizationId)
        {
            var organization = await organizationService.GetByIdAsync(organizationId);
            if (organization == null)
            {
                return NotFound();
            }

            var vm = new UpdateOrganizationDetailsViewModel
            {
                OrganizationId = organization.Id,
                Name = organization.Name,
                Address = organization.Address,
                PhoneNumber = organization.PhoneNumber
            };

            return View(vm);
        }

        // POST: Organization/UpdateDetails
        [Authorize(Roles = "SalesAdminSuper")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateDetails(UpdateOrganizationDetailsViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // You should reload the view model with any necessary data if you return here
                return View(model);
            }

            // 1. Get the ID of the user performing the action
            var ownerId = currentUserService.GetUserId();
            if (ownerId == Guid.Empty)
            {
                return Unauthorized();
            }

            // 2. Map ViewModel to DTO
            var dto = new OrganizationUpdateDto
            {
                OrganizationId = model.OrganizationId,
                Name = model.Name,
                Address = model.Address,
                PhoneNumber = model.PhoneNumber
            };

            // 3. Call the new auditable service method
            var success = await organizationService.UpdateOrganizationAsync(dto, ownerId);

            if (!success)
            {
                TempData["Message"] = "Error: Could not update organization.";
                return View(model);
            }

            TempData["Message"] = "Organization details updated successfully!";

            // Redirect to the OrganizationView, but you need its ID.
            // Assuming your OrganizationView action takes an 'id' parameter.
            return RedirectToAction("OrganizationView", "Organization", new { id = model.OrganizationId });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SalesAdminSuper")]
        public async Task<IActionResult> DeleteUser(int organizationId, Guid userId, Guid newUserId)
        {
            var adminId = currentUserService.GetUserId();
            var result = await organizationService.ReassignAndRemoveUserAsync(userId, organizationId, newUserId, adminId);
            if (result)
                TempData["Message"] = "User deleted successfully.";
            else
                TempData["Error"] = "Failed to delete user.";
            return RedirectToAction("OrganizationView", "Organization", new { organizationId });
        }
    }
}
