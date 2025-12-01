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
    public class OrganizationController(IOrganizationService organizationService,IConfiguration configuration, IInvitationService invitationService, IGmailService gmailService, ICurrentUserService currentUserService,IUserService userService) : Controller
    {
        public IActionResult Index()
        {
            
            return View();
        }

        public IActionResult Register()
        {
            var userId = currentUserService.GetUserId();

            if (!string.IsNullOrEmpty(organizationService.IsInOrganizationById(userId).Result?.Name))
            {
                return RedirectToAction("Index", "Organization");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(Organization organization)
        {
            
            var userId = currentUserService.GetUserId();

            await organizationService.CreateOrganizationAsync(organization, userId);

            
            if (!string.IsNullOrEmpty(organizationService.IsInOrganizationById(userId).Result?.Name))
            {
                var org = organizationService.IsInOrganizationById(userId).Result;

                var identity = new ClaimsIdentity(
                    User.Identity.AuthenticationType,
                    ClaimTypes.Name,
                    ClaimTypes.Role
                );

                identity.AddClaims(User.Claims);

                var oldOrgClaim = identity.FindFirst("OrganizationName");
                if (oldOrgClaim != null)
                {
                    identity.RemoveClaim(oldOrgClaim);
                }

                identity.AddClaim(new Claim("OrganizationName", org.Name));


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
            
            var organization = await organizationService.IsInOrganizationById(userId);
            if (organization == null)
            {
                TempData["Error"] = "You must be in an organization to send invitations.";
                return RedirectToAction("Index");
            }

            var invitation = await invitationService.CreateInvitationAsync(email, organization.Id, roleId, userId);

            if (invitation == null)
            {
                TempData["Message"] = "An active invitation has already been sent to this email address.";
                return RedirectToAction("Index"); 
            }

            string? adminId = configuration["Gmail:AdminEmailId"];
            await gmailService.SendEmailAsync(Guid.Parse(adminId ?? ""),email,"Invitation from SCRM",$"Hey, you just got invited to SCRM in {organization.Name}, log in to the SCRM using this link - {"https://salescrm.qzz.io/auth/login?invitationcode=" + invitation.InvitationCode}   ,            if new user use this link {"https://salescrm.qzz.io/auth/register?invitationcode=" + invitation.InvitationCode}","");

            TempData["Message"] = $"Invitation sent successfully to {email}!";
            return RedirectToAction("Index");
        }

        
        public async Task<IActionResult> OrganizationView()
        {
            var userId = currentUserService.GetUserId();

            if (userId == Guid.Empty) {
                return Unauthorized();
            }
            var user = await  userService.GetUserByIdAsync(userId);
            if (user.OrganizationId == null)
            {
                return NotFound();
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

       
        [Authorize(Roles = "SalesAdminSuper")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateDetails(UpdateOrganizationDetailsViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var ownerId = currentUserService.GetUserId();
            if (ownerId == Guid.Empty)
            {
                return Unauthorized();
            }

            var dto = new OrganizationUpdateDto
            {
                OrganizationId = model.OrganizationId,
                Name = model.Name,
                Address = model.Address,
                PhoneNumber = model.PhoneNumber
            };

            var success = await organizationService.UpdateOrganizationAsync(dto, ownerId);

            if (!success)
            {
                TempData["Message"] = "Error: Could not update organization.";
                return View(model);
            }

            TempData["Message"] = "Organization details updated successfully!";

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
