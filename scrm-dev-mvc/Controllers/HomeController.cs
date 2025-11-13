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

        [Authorize] 
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

            return View(new ErrorViewModel { RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public IActionResult LandingPage()
        {
            if (currentUserService.IsAuthenticated())
            {
                return RedirectToAction("Index", "Workspace");
            }

            return View();
        }
    }
}
