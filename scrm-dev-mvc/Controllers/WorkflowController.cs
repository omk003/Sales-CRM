using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using scrm_dev_mvc.DataAccess.Data;
using scrm_dev_mvc.Models;
using scrm_dev_mvc.Models.Enums; 
using scrm_dev_mvc.Models.ViewModels;
using scrm_dev_mvc.services;
using scrm_dev_mvc.services.Interfaces;
using scrm_dev_mvc.Utilities;

namespace scrm_dev_mvc.Controllers
{
    public class WorkflowController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IOrganizationService _organizationService; 
        private readonly ICurrentUserService _currentUserService;


        public WorkflowController(ApplicationDbContext context, IOrganizationService organizationService, ICurrentUserService currentUserService)
        {
            _context = context;
            _organizationService = organizationService;
            _currentUserService = currentUserService;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _currentUserService.GetUserId();
            var organization = await _organizationService.GetOrganizationViewModelByUserId(userId);
            if (organization == null) return Unauthorized();

            var workflows = await _context.Workflows
                .Include(w => w.Actions)
                .Where(w => w.OrganizationId == organization.OrganizationId)
                .ToListAsync();

            ViewData["IsAdmin"] = _currentUserService.IsInRole("SalesAdminSuper") || _currentUserService.IsInRole("SalesAdmin");

            return View(workflows);
        }

        [Authorize(Roles = "SalesAdminSuper,SalesAdmin")]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var viewModel = new WorkflowCreateViewModel
            {
                AvailableTriggers = Enum.GetValues(typeof(WorkflowTrigger))
                    .Cast<WorkflowTrigger>()
                    .Select(e => new SelectListItem { Text = e.GetDisplayName(), Value = ((int)e).ToString() }),

                AvailablePriorities = (await _context.Priorities.ToListAsync())
                    .Select(p => new SelectListItem { Text = p.Name, Value = p.Id.ToString() }),

                AvailableTaskStatuses = (await _context.TaskStatuses.ToListAsync())
                    .Select(s => new SelectListItem { Text = s.StatusName, Value = s.Id.ToString() }),

                AvailableLeadStatuses = Enum.GetValues(typeof(LeadStatusEnum))
                    .Cast<LeadStatusEnum>()
                    .Select(e => new SelectListItem { Text = e.GetDisplayName(), Value = ((int)e).ToString() }),

                AvailableLifeCycleStages = (await _context.Lifecycles.ToListAsync())
                    .Select(s => new SelectListItem { Text = s.LifeCycleStageName, Value = s.Id.ToString() })
            };

            return View(viewModel);
        }

        [Authorize(Roles = "SalesAdminSuper,SalesAdmin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(WorkflowCreateViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                await PopulateViewModelDropdownsAsync(viewModel);
                return View(viewModel);
            }
            var userId = _currentUserService.GetUserId();

            var organization = await _organizationService.GetOrganizationViewModelByUserId(userId);
            if (organization == null) return Unauthorized();

            var workflow = new Workflow
            {
                Name = viewModel.Name,
                Event = viewModel.Trigger,
                IsActive = true,
                OrganizationId = organization.OrganizationId
            };

            if (viewModel.Action_ChangeLeadStatus)
            {
                var parameters = new { NewLeadStatus = viewModel.ChangeLeadStatus_NewStatusId };

                workflow.Actions.Add(new WorkflowAction
                {
                    ActionType = WorkflowActionType.ChangeLeadStatus,
                    ParametersJson = Newtonsoft.Json.JsonConvert.SerializeObject(parameters),
                    ConditionJson = "{}" 
                });
            }

            if (viewModel.Action_ChangeLifeCycleStage)
            {
                var parameters = new { NewStageId = viewModel.ChangeLifeCycleStage_NewStageId };

                workflow.Actions.Add(new WorkflowAction
                {
                    ActionType = WorkflowActionType.ChangeLifeCycleStage,
                    ParametersJson = Newtonsoft.Json.JsonConvert.SerializeObject(parameters),
                    ConditionJson = "{}" 
                });
            }

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

                var conditionDict = new Dictionary<string, string>();
                if (!string.IsNullOrWhiteSpace(viewModel.Task_ConditionTaskType))
                {
                    conditionDict["TaskType"] = viewModel.Task_ConditionTaskType;
                }
                if (viewModel.Task_ConditionLeadStatus.HasValue)
                {
                    conditionDict["LeadStatusId"] = viewModel.Task_ConditionLeadStatus.Value.ToString();
                }

                var action = new WorkflowAction
                {
                    ActionType = WorkflowActionType.CreateTask,
                    ParametersJson = Newtonsoft.Json.JsonConvert.SerializeObject(parameters),
                    ConditionJson = conditionDict.Count > 0
                        ? Newtonsoft.Json.JsonConvert.SerializeObject(conditionDict)
                        : "{}" 
                };

                workflow.Actions.Add(action);
            }

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

        [Authorize(Roles = "SalesAdminSuper,SalesAdmin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var userId = _currentUserService.GetUserId();

            var organization = await _organizationService.GetOrganizationViewModelByUserId(userId);
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

            workflow.IsActive = !workflow.IsActive;

            _context.Workflows.Update(workflow);
            await _context.SaveChangesAsync();

            TempData["Message"] = $"Workflow '{workflow.Name}' has been {(workflow.IsActive ? "activated" : "deactivated")}.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "SalesAdminSuper,SalesAdmin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = _currentUserService.GetUserId();

            var organization = await _organizationService.GetOrganizationViewModelByUserId(userId);
            if (organization == null)
            {
                return Unauthorized();
            }

            var workflow = await _context.Workflows
                .Include(w => w.Actions)
                .FirstOrDefaultAsync(w => w.Id == id && w.OrganizationId == organization.OrganizationId);

            if (workflow == null)
            {
                return NotFound();
            }

            _context.Workflows.Remove(workflow);
            await _context.SaveChangesAsync();

            TempData["Message"] = $"Workflow '{workflow.Name}' has been deleted.";
            return RedirectToAction(nameof(Index));
        }


        [Authorize(Roles = "SalesAdminSuper,SalesAdmin")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = _currentUserService.GetUserId();
            var organization = await _organizationService.GetOrganizationViewModelByUserId(userId);
            if (organization == null) return Unauthorized();

            var workflow = await _context.Workflows
                .Include(w => w.Actions) 
                .FirstOrDefaultAsync(w => w.Id == id && w.OrganizationId == organization.OrganizationId);

            if (workflow == null)
            {
                return NotFound();
            }

            var viewModel = new WorkflowEditViewModel
            {
                Id = workflow.Id,
                Name = workflow.Name,
                Trigger = workflow.Event
            };

            var createTaskAction = workflow.Actions.FirstOrDefault(a => a.ActionType == WorkflowActionType.CreateTask);
            if (createTaskAction != null)
            {
                viewModel.Action_CreateTask = true;
                var parameters = JsonConvert.DeserializeObject<CreateTaskParams>(createTaskAction.ParametersJson);
                if (parameters != null)
                {
                    viewModel.Task_Title = parameters.Title;
                    viewModel.Task_DaysDue = parameters.DaysDue;
                    viewModel.Task_TaskType = parameters.TaskType;
                    viewModel.Task_PriorityId = parameters.PriorityId;
                    viewModel.Task_StatusId = parameters.StatusId;
                }
            }

            var changeStatusAction = workflow.Actions.FirstOrDefault(a => a.ActionType == WorkflowActionType.ChangeLeadStatus);
            if (changeStatusAction != null)
            {
                viewModel.Action_ChangeLeadStatus = true;
                var parameters = JsonConvert.DeserializeObject<ChangeLeadStatusParams>(changeStatusAction.ParametersJson);
                if (parameters != null)
                {
                    viewModel.ChangeLeadStatus_NewStatusId = parameters.NewLeadStatus;
                }
            }

            var changeStageAction = workflow.Actions.FirstOrDefault(a => a.ActionType == WorkflowActionType.ChangeLifeCycleStage);
            if (changeStageAction != null)
            {
                viewModel.Action_ChangeLifeCycleStage = true;
                var parameters = JsonConvert.DeserializeObject<ChangeLifeCycleStageParams>(changeStageAction.ParametersJson);
                if (parameters != null)
                {
                    viewModel.ChangeLifeCycleStage_NewStageId = parameters.NewStageId;
                }
            }

            await PopulateViewModelDropdownsAsync(viewModel);
            return View(viewModel);
        }

        [Authorize(Roles = "SalesAdminSuper,SalesAdmin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, WorkflowEditViewModel viewModel)
        {
            if (id != viewModel.Id)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                await PopulateViewModelDropdownsAsync(viewModel);
                return View(viewModel);
            }

            var userId = _currentUserService.GetUserId();
            var organization = await _organizationService.GetOrganizationViewModelByUserId(userId);
            if (organization == null) return Unauthorized();

            var workflowToUpdate = await _context.Workflows
                .Include(w => w.Actions)
                .FirstOrDefaultAsync(w => w.Id == id && w.OrganizationId == organization.OrganizationId);

            if (workflowToUpdate == null)
            {
                return NotFound();
            }

            workflowToUpdate.Name = viewModel.Name;
            workflowToUpdate.Event = viewModel.Trigger;

            
            _context.WorkflowActions.RemoveRange(workflowToUpdate.Actions);
            workflowToUpdate.Actions.Clear();

            if (viewModel.Action_ChangeLeadStatus)
            {
                var parameters = new { NewLeadStatus = viewModel.ChangeLeadStatus_NewStatusId };
                workflowToUpdate.Actions.Add(new WorkflowAction
                {
                    ActionType = WorkflowActionType.ChangeLeadStatus,
                    ParametersJson = JsonConvert.SerializeObject(parameters)
                });
            }

            if (viewModel.Action_ChangeLifeCycleStage)
            {
                var parameters = new { NewStageId = viewModel.ChangeLifeCycleStage_NewStageId };
                workflowToUpdate.Actions.Add(new WorkflowAction
                {
                    ActionType = WorkflowActionType.ChangeLifeCycleStage,
                    ParametersJson = JsonConvert.SerializeObject(parameters)
                });
            }

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
                workflowToUpdate.Actions.Add(new WorkflowAction
                {
                    ActionType = WorkflowActionType.CreateTask,
                    ParametersJson = JsonConvert.SerializeObject(parameters)
                });
            }

            _context.Workflows.Update(workflowToUpdate);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Workflow updated successfully!";
            return RedirectToAction(nameof(Index));
        }


        private class CreateTaskParams
        {
            public string Title { get; set; }
            public int DaysDue { get; set; }
            public string TaskType { get; set; }
            public string AssignedTo { get; set; }
            public int PriorityId { get; set; }
            public int StatusId { get; set; }
        }
        private class ChangeLeadStatusParams
        {
            public LeadStatusEnum NewLeadStatus { get; set; }
        }
        private class ChangeLifeCycleStageParams
        {
            public int NewStageId { get; set; }
        }
    }
}
