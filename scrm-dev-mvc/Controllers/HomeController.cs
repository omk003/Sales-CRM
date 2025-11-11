using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using scrm_dev_mvc.Models;
using scrm_dev_mvc.services.Interfaces;
using System.Diagnostics;
using System.Security.Claims;

namespace scrm_dev_mvc.Controllers
{
    public class HomeController(ICurrentUserService currentUserService) : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [Authorize] // Requires any authenticated user
        public IActionResult Secret()
        {
            var userId = currentUserService.GetUserId();
            ViewBag.Message = $"Welcome to the secret page! Your User ID is: {userId}";
            return View();
        }

      
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            var exceptionHandlerPathFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();

            // You would log exceptionHandlerPathFeature.Error here with a real logging framework

            return View(new ErrorViewModel { RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public IActionResult LandingPage()
        {
            // Check if the user's identity is authenticated
            if (currentUserService.IsAuthenticated())
            {
                // If logged in, redirect to the main dashboard
                return RedirectToAction("Index", "Home");
            }

            // If not logged in, show the landing page view
            return View();
        }
    }
}
