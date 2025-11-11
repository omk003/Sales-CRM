using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using scrm_dev_mvc.Data; // Your DbContext
using scrm_dev_mvc.DataAccess.Data;
using scrm_dev_mvc.Models; // Your Models
using scrm_dev_mvc.Models.Enums; // Your Enums
using scrm_dev_mvc.Models.ViewModels; // We'll create this
using scrm_dev_mvc.services;
using scrm_dev_mvc.services.Interfaces; // For IOrganizationService
using scrm_dev_mvc.Utilities;
using System.Text.Json; // For JSON

namespace scrm_dev_mvc.Controllers
{
    public class WorkflowController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IOrganizationService _organizationService; 


        public WorkflowController(ApplicationDbContext context, IOrganizationService organizationService)
        {
            _context = context;
            _organizationService = organizationService;
        }

        // 1. INDEX: Shows all workflows
        public async Task<IActionResult> Index()
        {
            var organization = await _organizationService.GetOrganizationViewModelByUserId(User.GetUserId());
            if (organization == null) return Unauthorized();

            var workflows = await _context.Workflows
                .Include(w => w.Actions)
                .Where(w => w.OrganizationId == organization.OrganizationId)
                .ToListAsync();

            return View(workflows);
        }

        // 2. CREATE (GET): Shows the "Create" form
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var viewModel = new WorkflowCreateViewModel
            {
                // Populate dropdowns
                AvailableTriggers = Enum.GetValues(typeof(WorkflowTrigger))
                    .Cast<WorkflowTrigger>()
                    .Select(e => new SelectListItem { Text = e.GetDisplayName(), Value = ((int)e).ToString() }),

                //AvailableActionTypes = Enum.GetValues(typeof(WorkflowActionType))
                //    .Cast<WorkflowActionType>()
                //    .Select(e => new SelectListItem { Text = e.GetDisplayName(), Value = ((int)e).ToString() }),

                // For the "Create Task" parameters
                AvailablePriorities = (await _context.Priorities.ToListAsync())
                    .Select(p => new SelectListItem { Text = p.Name, Value = p.Id.ToString() }),

                AvailableTaskStatuses = (await _context.TaskStatuses.ToListAsync())
                    .Select(s => new SelectListItem { Text = s.StatusName, Value = s.Id.ToString() }),

                // For the "Update Status" parameters
                AvailableLeadStatuses = Enum.GetValues(typeof(LeadStatusEnum))
                    .Cast<LeadStatusEnum>()
                    .Select(e => new SelectListItem { Text = e.GetDisplayName(), Value = ((int)e).ToString() }),

                AvailableLifeCycleStages = (await _context.Lifecycles.ToListAsync())
                    .Select(s => new SelectListItem { Text = s.LifeCycleStageName, Value = s.Id.ToString() })
            };

            return View(viewModel);
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(WorkflowCreateViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                //_logger.LogWarning("ModelState is invalid. Repopulating dropdowns.");
                await PopulateViewModelDropdownsAsync(viewModel);
                // (You would repopulate all dropdowns here on failure)
                return View(viewModel);
            }

            var organization = await _organizationService.GetOrganizationViewModelByUserId(User.GetUserId());
            if (organization == null) return Unauthorized();

            // 1. Create ONE Workflow
            var workflow = new Workflow
            {
                Name = viewModel.Name,
                Event = viewModel.Trigger,
                IsActive = true,
                OrganizationId = organization.OrganizationId
            };

            // 2. Check for "Change Lead Status" action
            if (viewModel.Action_ChangeLeadStatus)
            {
                var parameters = new { NewStatus = viewModel.ChangeLeadStatus_NewStatusId };
                workflow.Actions.Add(new WorkflowAction
                {
                    ActionType = WorkflowActionType.ChangeLeadStatus,
                    ParametersJson = JsonSerializer.Serialize(parameters)
                });
            }

            // 3. Check for "Change LifeCycle Stage" action
            if (viewModel.Action_ChangeLifeCycleStage)
            {
                var parameters = new { NewStageId = viewModel.ChangeLifeCycleStage_NewStageId };
                workflow.Actions.Add(new WorkflowAction
                {
                    ActionType = WorkflowActionType.ChangeLifeCycleStage,
                    ParametersJson = JsonSerializer.Serialize(parameters)
                });
            }

            // 4. Check for "Create Task" action
            if (viewModel.Action_CreateTask)
            {
                var parameters = new
                {
                    Title = viewModel.Task_Title,
                    DaysDue = viewModel.Task_DaysDue,
                    TaskType = viewModel.Task_TaskType,
                    AssignedTo = "ContactOwner",
                    PriorityId = viewModel.Task_PriorityId,
                    StatusId = viewModel.Task_StatusId
                };
                workflow.Actions.Add(new WorkflowAction
                {
                    ActionType = WorkflowActionType.CreateTask,
                    ParametersJson = JsonSerializer.Serialize(parameters)
                });
            }

            // 5. Save the Workflow (and all its actions)
            _context.Workflows.Add(workflow);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Workflow created successfully!";
            return RedirectToAction(nameof(Index));
        }



        private async System.Threading.Tasks.Task PopulateViewModelDropdownsAsync(WorkflowCreateViewModel viewModel)
        {
            viewModel.AvailableTriggers = Enum.GetValues(typeof(WorkflowTrigger))
                .Cast<WorkflowTrigger>()
                .Select(e => new SelectListItem { Text = e.GetDisplayName(), Value = ((int)e).ToString() });

            viewModel.AvailableLeadStatuses = Enum.GetValues(typeof(LeadStatusEnum))
                .Cast<LeadStatusEnum>()
                .Select(e => new SelectListItem { Text = e.GetDisplayName(), Value = ((int)e).ToString() });

            viewModel.AvailableLifeCycleStages = (await _context.Lifecycles.ToListAsync())
                 .Select(s => new SelectListItem { Text = s.LifeCycleStageName, Value = s.Id.ToString() });

            viewModel.AvailablePriorities = (await _context.Priorities.ToListAsync())
                    .Select(p => new SelectListItem { Text = p.Name, Value = p.Id.ToString() });

            viewModel.AvailableTaskStatuses = (await _context.TaskStatuses.ToListAsync())
                    .Select(s => new SelectListItem { Text = s.StatusName, Value = s.Id.ToString() });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        // Make sure this is protected, e.g., [Authorize]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var organization = await _organizationService.GetOrganizationViewModelByUserId(User.GetUserId());
            if (organization == null)
            {
                return Unauthorized();
            }

            var workflow = await _context.Workflows.FirstOrDefaultAsync(
                w => w.Id == id && w.OrganizationId == organization.OrganizationId);

            if (workflow == null)
            {
                return NotFound();
            }

            // Flip the boolean
            workflow.IsActive = !workflow.IsActive;

            _context.Workflows.Update(workflow);
            await _context.SaveChangesAsync();

            TempData["Message"] = $"Workflow '{workflow.Name}' has been {(workflow.IsActive ? "activated" : "deactivated")}.";
            return RedirectToAction(nameof(Index));
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var organization = await _organizationService.GetOrganizationViewModelByUserId(User.GetUserId());
            if (organization == null)
            {
                return Unauthorized();
            }

            // Find the workflow, including its actions, to make sure we delete them all
            var workflow = await _context.Workflows
                .Include(w => w.Actions)
                .FirstOrDefaultAsync(w => w.Id == id && w.OrganizationId == organization.OrganizationId);

            if (workflow == null)
            {
                return NotFound();
            }

            // EF Core will automatically delete the related 'Actions'
            // because they are part of the workflow object we loaded.
            _context.Workflows.Remove(workflow);
            await _context.SaveChangesAsync();

            TempData["Message"] = $"Workflow '{workflow.Name}' has been deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}
