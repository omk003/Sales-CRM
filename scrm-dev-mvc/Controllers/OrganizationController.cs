using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using scrm_dev_mvc.Models;
using scrm_dev_mvc.services;
using scrm_dev_mvc.Services;
using System.Security.Claims;

namespace scrm_dev_mvc.Controllers
{
    [Authorize]
    public class OrganizationController(IOrganizationService organizationService,IConfiguration configuration, IInvitationService invitationService, IGmailService gmailService) : Controller
    {
        public async Task<IActionResult> Index()
        {
            var organizations = await organizationService.GetAllOrganizationsAsync();
            return View(organizations);
        }

        public IActionResult Register()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // check if he is in organization
            if (!string.IsNullOrEmpty(organizationService.IsInOrganizationById(Guid.Parse(userId)).Result?.Name))
            {
                TempData["Error"] = "You are already in an organization.";
                return RedirectToAction("Index", "Organization");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(Organization organization)
        {
            // Get the user who is currently logged in
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            
            // Pass User in Service layer of organization
            await organizationService.CreateOrganizationAsync(organization, Guid.Parse(userId));

            if (string.IsNullOrEmpty(organizationService.IsInOrganizationById(Guid.Parse(userId)).Result?.Name))
            {
                var org = organizationService.IsInOrganizationById(Guid.Parse(userId)).Result;
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
        public async Task<IActionResult> SendInvitation(string email, int roleId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            // Get the organization of the user sending the invitation
            var organization = await organizationService.IsInOrganizationById(Guid.Parse(userId));
            if (organization == null)
            {
                TempData["Error"] = "You must be in an organization to send invitations.";
                return RedirectToAction("Index");
            }

            // 1. Create the invitation in the database
            var invitation = await invitationService.CreateInvitationAsync(email, organization.Id, roleId);
            string? adminId = configuration["Gmail:AdminUserId"];
            // 2. Send the invitation via email
            await gmailService.SendEmailAsync(Guid.Parse(adminId ?? ""),email,"Invitation from SCRM",$"Hey, you just got invited to SCRM in {organization.Name}, log in to the SCRM using this link - {"https://maudlinly-nonreactive-arturo.ngrok-free.dev/auth/login?invitationcode=" + invitation.InvitationCode}   ,            if new user use this link {"https://maudlinly-nonreactive-arturo.ngrok-free.dev/auth/register?invitationcode=" + invitation.InvitationCode}","");

            TempData["Message"] = $"Invitation sent successfully to {email}!";
            return RedirectToAction("Index");
        }

    }
}
