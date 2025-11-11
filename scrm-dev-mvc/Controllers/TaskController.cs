using Microsoft.AspNetCore.Mvc;
using scrm_dev_mvc.Models.ViewModels;
using scrm_dev_mvc.services.Interfaces;
using System.Security.Claims;

namespace scrm_dev_mvc.Controllers
{
    public class TaskController : Controller
    {
        private readonly ITaskService _taskService;
        private readonly ILogger<TaskController> _logger;
        public TaskController(ITaskService taskService, ILogger<TaskController> logger)
        {
            _taskService = taskService;
            _logger = logger;
        }

        // [HttpGet] - No change
        public async Task<IActionResult> Create(int? contactId, int? companyId, int? dealId)
        {
            var viewModel = await _taskService.GetTaskCreateViewModelAsync(contactId, companyId, dealId);
            return PartialView("_CreateTaskModal", viewModel);
        }

        // [HttpPost] - Updated
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TaskCreateViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors)
                                          .Select(e => e.ErrorMessage);
                return BadRequest(new { message = "Validation failed.", errors = errors });
            }

            // --- NEW: Get the Owner's ID from the authenticated user ---
            var ownerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(ownerId))
            {
                return Unauthorized(new { message = "You must be logged in to create a task." });
            }

            // --- Pass the ownerId to the service ---
            var result = await _taskService.CreateTaskAsync(viewModel, Guid.Parse(ownerId));

            if (result.Success)
            {
                return Ok(new { message = result.Message });
            }
            else
            {
                return BadRequest(new { message = result.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Update(int taskId)
        {
            try
            {
                var viewModel = await _taskService.GetTaskUpdateViewModelAsync(taskId);
                return PartialView("_UpdateTaskModal", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting task for update.");
                return BadRequest(new { message = "Error loading task: " + ex.Message });
            }
        }

        // --- NEW POST ACTION ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(TaskUpdateViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors)
                                          .Select(e => e.ErrorMessage);
                return BadRequest(new { message = "Validation failed.", errors = errors });
            }

            var ownerIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(ownerIdString, out Guid ownerId))
            {
                return Unauthorized(new { message = "User ID claim is invalid." });
            }

            var result = await _taskService.UpdateTaskAndEntitiesAsync(viewModel, ownerId);

            if (result.Success)
            {
                return Ok(new { message = result.Message });
            }
            else
            {
                return BadRequest(new { message = result.Message });
            }
        }
    }
}