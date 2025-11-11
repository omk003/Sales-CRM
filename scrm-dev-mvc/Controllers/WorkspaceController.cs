using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using scrm_dev_mvc.services.Interfaces;
using System.Security.Claims;
using System.Threading.Tasks;

namespace scrm_dev_mvc.Controllers
{
    [Authorize] 
    public class WorkspaceController : Controller
    {
        private readonly IWorkspaceService _workspaceService;
        private readonly ILogger<WorkspaceController> _logger;
        private readonly ICurrentUserService _currentUserService;

        public WorkspaceController(IWorkspaceService workspaceService, ILogger<WorkspaceController> logger, ICurrentUserService currentUserService)
        {
            _workspaceService = workspaceService;
            _logger = logger;
            _currentUserService = currentUserService;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _currentUserService.GetUserId();
            if (userId == Guid.Empty)
            {
                return Challenge();
            }

            bool isAdmin = _currentUserService.IsInRole("SalesAdminSuper") || _currentUserService.IsInRole("SalesAdmin");

            var viewModel = await _workspaceService.GetWorkspaceDataAsync(userId, isAdmin);

            return View(viewModel);
        }
    }
}