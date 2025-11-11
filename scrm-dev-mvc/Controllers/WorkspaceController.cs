using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using scrm_dev_mvc.services.Interfaces;
using System.Security.Claims;
using System.Threading.Tasks;

namespace scrm_dev_mvc.Controllers
{
    [Authorize] // Make sure the user is logged in
    public class WorkspaceController : Controller
    {
        private readonly IWorkspaceService _workspaceService;
        private readonly ILogger<WorkspaceController> _logger;

        public WorkspaceController(IWorkspaceService workspaceService, ILogger<WorkspaceController> logger)
        {
            _workspaceService = workspaceService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            // Check if the user is an Admin
            bool isAdmin = User.IsInRole("SalesAdminSuper") || User.IsInRole("SalesAdmin");

            var viewModel = await _workspaceService.GetWorkspaceDataAsync(Guid.Parse(userId), isAdmin);

            return View(viewModel);
        }
    }
}