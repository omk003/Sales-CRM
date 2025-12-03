//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.Mvc.Rendering;
//using Microsoft.EntityFrameworkCore;
//using Newtonsoft.Json;
//using scrm_dev_mvc.DataAccess.Data;
//using scrm_dev_mvc.Models;
//using scrm_dev_mvc.Models.Enums; 
//using scrm_dev_mvc.Models.ViewModels;
//using scrm_dev_mvc.services;
//using scrm_dev_mvc.services.Interfaces;
//using scrm_dev_mvc.Utilities;

//namespace scrm_dev_mvc.Controllers
//{
//    public class WorkflowController : Controller
//    {
//        private readonly ApplicationDbContext _context;
//        private readonly IOrganizationService _organizationService; 
//        private readonly ICurrentUserService _currentUserService;


//        public WorkflowController(ApplicationDbContext context, IOrganizationService organizationService, ICurrentUserService currentUserService)
//        {
//            _context = context;
//            _organizationService = organizationService;
//            _currentUserService = currentUserService;
//        }

//        public async Task<IActionResult> Index()
//        {
//            var userId = _currentUserService.GetUserId();
//            var organization = await _organizationService.GetOrganizationViewModelByUserId(userId);
//            if (organization == null) return Unauthorized();

//            var workflows = await _context.Workflows
//                .Include(w => w.Actions)
//                .Where(w => w.OrganizationId == organization.OrganizationId)
//                .ToListAsync();

//            ViewData["IsAdmin"] = _currentUserService.IsInRole("SalesAdminSuper") || _currentUserService.IsInRole("SalesAdmin");

//            return View(workflows);
//        }

//        [Authorize(Roles = "SalesAdminSuper,SalesAdmin")]
//        [HttpGet]
//        public async Task<IActionResult> Create()
//        {
//            var viewModel = new WorkflowCreateViewModel
//            {
//                AvailableTriggers = Enum.GetValues(typeof(WorkflowTrigger))
//                    .Cast<WorkflowTrigger>()
//                    .Select(e => new SelectListItem { Text = e.GetDisplayName(), Value = ((int)e).ToString() }),

//                AvailablePriorities = (await _context.Priorities.ToListAsync())
//                    .Select(p => new SelectListItem { Text = p.Name, Value = p.Id.ToString() }),

//                AvailableTaskStatuses = (await _context.TaskStatuses.ToListAsync())
//                    .Select(s => new SelectListItem { Text = s.StatusName, Value = s.Id.ToString() }),

//                AvailableLeadStatuses = Enum.GetValues(typeof(LeadStatusEnum))
//                    .Cast<LeadStatusEnum>()
//                    .Select(e => new SelectListItem { Text = e.GetDisplayName(), Value = ((int)e).ToString() }),

//                AvailableLifeCycleStages = (await _context.Lifecycles.ToListAsync())
//                    .Select(s => new SelectListItem { Text = s.LifeCycleStageName, Value = s.Id.ToString() })
//            };

//            return View(viewModel);
//        }

//        [Authorize(Roles = "SalesAdminSuper,SalesAdmin")]
//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> Create(WorkflowCreateViewModel viewModel)
//        {
//            if (!ModelState.IsValid)
//            {
//                await PopulateViewModelDropdownsAsync(viewModel);
//                return View(viewModel);
//            }
//            var userId = _currentUserService.GetUserId();

//            var organization = await _organizationService.GetOrganizationViewModelByUserId(userId);
//            if (organization == null) return Unauthorized();

//            var workflow = new Workflow
//            {
//                Name = viewModel.Name,
//                Event = viewModel.Trigger,
//                IsActive = true,
//                OrganizationId = organization.OrganizationId
//            };

//            if (viewModel.Action_ChangeLeadStatus)
//            {
//                var parameters = new { NewLeadStatus = viewModel.ChangeLeadStatus_NewStatusId };

//                workflow.Actions.Add(new WorkflowAction
//                {
//                    ActionType = WorkflowActionType.ChangeLeadStatus,
//                    ParametersJson = Newtonsoft.Json.JsonConvert.SerializeObject(parameters),
//                    ConditionJson = "{}" 
//                });
//            }

//            if (viewModel.Action_ChangeLifeCycleStage)
//            {
//                var parameters = new { NewStageId = viewModel.ChangeLifeCycleStage_NewStageId };

//                workflow.Actions.Add(new WorkflowAction
//                {
//                    ActionType = WorkflowActionType.ChangeLifeCycleStage,
//                    ParametersJson = Newtonsoft.Json.JsonConvert.SerializeObject(parameters),
//                    ConditionJson = "{}" 
//                });
//            }

//            if (viewModel.Action_CreateTask)
//            {
//                var parameters = new
//                {
//                    Title = viewModel.Task_Title,
//                    DaysDue = viewModel.Task_DaysDue,
//                    TaskType = viewModel.Task_TaskType,
//                    AssignedTo = "ContactOwner",
//                    PriorityId = viewModel.Task_PriorityId,
//                    StatusId = viewModel.Task_StatusId
//                };

//                var conditionDict = new Dictionary<string, string>();
//                if (!string.IsNullOrWhiteSpace(viewModel.Task_ConditionTaskType))
//                {
//                    conditionDict["TaskType"] = viewModel.Task_ConditionTaskType;
//                }
//                if (viewModel.Task_ConditionLeadStatus.HasValue)
//                {
//                    conditionDict["LeadStatusId"] = viewModel.Task_ConditionLeadStatus.Value.ToString();
//                }

//                var action = new WorkflowAction
//                {
//                    ActionType = WorkflowActionType.CreateTask,
//                    ParametersJson = Newtonsoft.Json.JsonConvert.SerializeObject(parameters),
//                    ConditionJson = conditionDict.Count > 0
//                        ? Newtonsoft.Json.JsonConvert.SerializeObject(conditionDict)
//                        : "{}" 
//                };

//                workflow.Actions.Add(action);
//            }

//            _context.Workflows.Add(workflow);
//            await _context.SaveChangesAsync();

//            TempData["Message"] = "Workflow created successfully!";
//            return RedirectToAction(nameof(Index));
//        }

//        private async System.Threading.Tasks.Task PopulateViewModelDropdownsAsync(WorkflowCreateViewModel viewModel)
//        {
//            viewModel.AvailableTriggers = Enum.GetValues(typeof(WorkflowTrigger))
//                .Cast<WorkflowTrigger>()
//                .Select(e => new SelectListItem { Text = e.GetDisplayName(), Value = ((int)e).ToString() });

//            viewModel.AvailableLeadStatuses = Enum.GetValues(typeof(LeadStatusEnum))
//                .Cast<LeadStatusEnum>()
//                .Select(e => new SelectListItem { Text = e.GetDisplayName(), Value = ((int)e).ToString() });

//            viewModel.AvailableLifeCycleStages = (await _context.Lifecycles.ToListAsync())
//                 .Select(s => new SelectListItem { Text = s.LifeCycleStageName, Value = s.Id.ToString() });

//            viewModel.AvailablePriorities = (await _context.Priorities.ToListAsync())
//                    .Select(p => new SelectListItem { Text = p.Name, Value = p.Id.ToString() });

//            viewModel.AvailableTaskStatuses = (await _context.TaskStatuses.ToListAsync())
//                    .Select(s => new SelectListItem { Text = s.StatusName, Value = s.Id.ToString() });
//        }

//        [Authorize(Roles = "SalesAdminSuper,SalesAdmin")]
//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> ToggleActive(int id)
//        {
//            var userId = _currentUserService.GetUserId();

//            var organization = await _organizationService.GetOrganizationViewModelByUserId(userId);
//            if (organization == null)
//            {
//                return Unauthorized();
//            }

//            var workflow = await _context.Workflows.FirstOrDefaultAsync(
//                w => w.Id == id && w.OrganizationId == organization.OrganizationId);

//            if (workflow == null)
//            {
//                return NotFound();
//            }

//            workflow.IsActive = !workflow.IsActive;

//            _context.Workflows.Update(workflow);
//            await _context.SaveChangesAsync();

//            TempData["Message"] = $"Workflow '{workflow.Name}' has been {(workflow.IsActive ? "activated" : "deactivated")}.";
//            return RedirectToAction(nameof(Index));
//        }

//        [Authorize(Roles = "SalesAdminSuper,SalesAdmin")]
//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> Delete(int id)
//        {
//            var userId = _currentUserService.GetUserId();

//            var organization = await _organizationService.GetOrganizationViewModelByUserId(userId);
//            if (organization == null)
//            {
//                return Unauthorized();
//            }

//            var workflow = await _context.Workflows
//                .Include(w => w.Actions)
//                .FirstOrDefaultAsync(w => w.Id == id && w.OrganizationId == organization.OrganizationId);

//            if (workflow == null)
//            {
//                return NotFound();
//            }

//            _context.Workflows.Remove(workflow);
//            await _context.SaveChangesAsync();

//            TempData["Message"] = $"Workflow '{workflow.Name}' has been deleted.";
//            return RedirectToAction(nameof(Index));
//        }


//        [Authorize(Roles = "SalesAdminSuper,SalesAdmin")]
//        [HttpGet]
//        public async Task<IActionResult> Edit(int id)
//        {
//            var userId = _currentUserService.GetUserId();
//            var organization = await _organizationService.GetOrganizationViewModelByUserId(userId);
//            if (organization == null) return Unauthorized();

//            var workflow = await _context.Workflows
//                .Include(w => w.Actions) 
//                .FirstOrDefaultAsync(w => w.Id == id && w.OrganizationId == organization.OrganizationId);

//            if (workflow == null)
//            {
//                return NotFound();
//            }

//            var viewModel = new WorkflowEditViewModel
//            {
//                Id = workflow.Id,
//                Name = workflow.Name,
//                Trigger = workflow.Event
//            };

//            var createTaskAction = workflow.Actions.FirstOrDefault(a => a.ActionType == WorkflowActionType.CreateTask);
//            if (createTaskAction != null)
//            {
//                viewModel.Action_CreateTask = true;
//                var parameters = JsonConvert.DeserializeObject<CreateTaskParams>(createTaskAction.ParametersJson);
//                if (parameters != null)
//                {
//                    viewModel.Task_Title = parameters.Title;
//                    viewModel.Task_DaysDue = parameters.DaysDue;
//                    viewModel.Task_TaskType = parameters.TaskType;
//                    viewModel.Task_PriorityId = parameters.PriorityId;
//                    viewModel.Task_StatusId = parameters.StatusId;
//                }
//            }

//            var changeStatusAction = workflow.Actions.FirstOrDefault(a => a.ActionType == WorkflowActionType.ChangeLeadStatus);
//            if (changeStatusAction != null)
//            {
//                viewModel.Action_ChangeLeadStatus = true;
//                var parameters = JsonConvert.DeserializeObject<ChangeLeadStatusParams>(changeStatusAction.ParametersJson);
//                if (parameters != null)
//                {
//                    viewModel.ChangeLeadStatus_NewStatusId = parameters.NewLeadStatus;
//                }
//            }

//            var changeStageAction = workflow.Actions.FirstOrDefault(a => a.ActionType == WorkflowActionType.ChangeLifeCycleStage);
//            if (changeStageAction != null)
//            {
//                viewModel.Action_ChangeLifeCycleStage = true;
//                var parameters = JsonConvert.DeserializeObject<ChangeLifeCycleStageParams>(changeStageAction.ParametersJson);
//                if (parameters != null)
//                {
//                    viewModel.ChangeLifeCycleStage_NewStageId = parameters.NewStageId;
//                }
//            }

//            await PopulateViewModelDropdownsAsync(viewModel);
//            return View(viewModel);
//        }

//        [Authorize(Roles = "SalesAdminSuper,SalesAdmin")]
//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> Edit(int id, WorkflowEditViewModel viewModel)
//        {
//            if (id != viewModel.Id)
//            {
//                return BadRequest();
//            }

//            if (!ModelState.IsValid)
//            {
//                await PopulateViewModelDropdownsAsync(viewModel);
//                return View(viewModel);
//            }

//            var userId = _currentUserService.GetUserId();
//            var organization = await _organizationService.GetOrganizationViewModelByUserId(userId);
//            if (organization == null) return Unauthorized();

//            var workflowToUpdate = await _context.Workflows
//                .Include(w => w.Actions)
//                .FirstOrDefaultAsync(w => w.Id == id && w.OrganizationId == organization.OrganizationId);

//            if (workflowToUpdate == null)
//            {
//                return NotFound();
//            }

//            workflowToUpdate.Name = viewModel.Name;
//            workflowToUpdate.Event = viewModel.Trigger;


//            _context.WorkflowActions.RemoveRange(workflowToUpdate.Actions);
//            workflowToUpdate.Actions.Clear();

//            if (viewModel.Action_ChangeLeadStatus)
//            {
//                var parameters = new { NewLeadStatus = viewModel.ChangeLeadStatus_NewStatusId };
//                workflowToUpdate.Actions.Add(new WorkflowAction
//                {
//                    ActionType = WorkflowActionType.ChangeLeadStatus,
//                    ParametersJson = JsonConvert.SerializeObject(parameters)
//                });
//            }

//            if (viewModel.Action_ChangeLifeCycleStage)
//            {
//                var parameters = new { NewStageId = viewModel.ChangeLifeCycleStage_NewStageId };
//                workflowToUpdate.Actions.Add(new WorkflowAction
//                {
//                    ActionType = WorkflowActionType.ChangeLifeCycleStage,
//                    ParametersJson = JsonConvert.SerializeObject(parameters)
//                });
//            }

//            if (viewModel.Action_CreateTask)
//            {
//                var parameters = new
//                {
//                    Title = viewModel.Task_Title,
//                    DaysDue = viewModel.Task_DaysDue,
//                    TaskType = viewModel.Task_TaskType,
//                    AssignedTo = "ContactOwner",
//                    PriorityId = viewModel.Task_PriorityId,
//                    StatusId = viewModel.Task_StatusId
//                };
//                workflowToUpdate.Actions.Add(new WorkflowAction
//                {
//                    ActionType = WorkflowActionType.CreateTask,
//                    ParametersJson = JsonConvert.SerializeObject(parameters)
//                });
//            }

//            _context.Workflows.Update(workflowToUpdate);
//            await _context.SaveChangesAsync();

//            TempData["Message"] = "Workflow updated successfully!";
//            return RedirectToAction(nameof(Index));
//        }


//        private class CreateTaskParams
//        {
//            public string Title { get; set; }
//            public int DaysDue { get; set; }
//            public string TaskType { get; set; }
//            public string AssignedTo { get; set; }
//            public int PriorityId { get; set; }
//            public int StatusId { get; set; }
//        }
//        private class ChangeLeadStatusParams
//        {
//            public LeadStatusEnum NewLeadStatus { get; set; }
//        }
//        private class ChangeLifeCycleStageParams
//        {
//            public int NewStageId { get; set; }
//        }
//    }
//}


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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
            var viewModel = new WorkflowCreateViewModel();
            await PopulateViewModelDropdownsAsync(viewModel);
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

            string sharedConditionJson = GetWorkflowConditionJson(viewModel.Task_ConditionTaskType, viewModel.Task_ConditionLeadStatus);


            var workflow = new Workflow
            {
                Name = viewModel.Name,
                Event = viewModel.Trigger,
                IsActive = true,
                OrganizationId = organization.OrganizationId
            };

            if (viewModel.Action_ChangeLeadStatus)
            {
                workflow.Actions.Add(CreateChangeLeadStatusAction(viewModel.ChangeLeadStatus_NewStatusId, sharedConditionJson));
            }

            if (viewModel.Action_ChangeLifeCycleStage)
            {
                workflow.Actions.Add(CreateChangeLifeCycleStageAction(viewModel.ChangeLifeCycleStage_NewStageId, sharedConditionJson));
            }

            if (viewModel.Action_CreateTask)
            {
                workflow.Actions.Add(CreateCreateTaskAction(
                    viewModel.Task_Title,
                    viewModel.Task_DaysDue,
                    viewModel.Task_TaskType,
                    viewModel.Task_PriorityId,
                    viewModel.Task_StatusId,
                    sharedConditionJson)); 
            }

            _context.Workflows.Add(workflow);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Workflow created successfully!";
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

            if (workflow == null) return NotFound();

            var viewModel = new WorkflowEditViewModel
            {
                Id = workflow.Id,
                Name = workflow.Name,
                Trigger = workflow.Event
            };

            bool conditionLoaded = false;

            foreach (var action in workflow.Actions)
            {
                switch (action.ActionType)
                {
                    case WorkflowActionType.CreateTask:
                        viewModel.Action_CreateTask = true;

                        var taskParams = JsonConvert.DeserializeObject<TaskActionParams>(action.ParametersJson);
                        if (taskParams != null)
                        {
                            viewModel.Task_Title = taskParams.Title;
                            viewModel.Task_DaysDue = taskParams.DaysDue;
                            viewModel.Task_TaskType = taskParams.TaskType;
                            viewModel.Task_PriorityId = taskParams.PriorityId;
                            viewModel.Task_StatusId = taskParams.StatusId;
                        }

                        if (!conditionLoaded && !string.IsNullOrEmpty(action.ConditionJson) && action.ConditionJson != "{}")
                        {
                            var conditionDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(action.ConditionJson);

                            if (conditionDict != null)
                            {
                                if (conditionDict.TryGetValue("TaskType", out string conditionTaskType))
                                {
                                    viewModel.Task_ConditionTaskType = conditionTaskType;
                                }

                                if (conditionDict.TryGetValue("LeadStatusId", out string conditionLeadStatusId) &&
                                    int.TryParse(conditionLeadStatusId, out int statusId))
                                {
                                    viewModel.Task_ConditionLeadStatus = statusId;
                                }
                                conditionLoaded = true;
                            }
                        }
                        break;

                    case WorkflowActionType.ChangeLeadStatus:
                        viewModel.Action_ChangeLeadStatus = true;
                        var statusParams = JsonConvert.DeserializeObject<LeadStatusActionParams>(action.ParametersJson);
                        if (statusParams != null)
                        {
                            viewModel.ChangeLeadStatus_NewStatusId = statusParams.NewLeadStatus;
                        }
                        break;

                    case WorkflowActionType.ChangeLifeCycleStage:
                        viewModel.Action_ChangeLifeCycleStage = true;
                        var stageParams = JsonConvert.DeserializeObject<LifeCycleStageActionParams>(action.ParametersJson);
                        if (stageParams != null)
                        {
                            viewModel.ChangeLifeCycleStage_NewStageId = stageParams.NewStageId;
                        }
                        break;
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
            if (id != viewModel.Id) return BadRequest();

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

            if (workflowToUpdate == null) return NotFound();

            workflowToUpdate.Name = viewModel.Name;
            workflowToUpdate.Event = viewModel.Trigger;

            _context.WorkflowActions.RemoveRange(workflowToUpdate.Actions);
            workflowToUpdate.Actions.Clear();

            string sharedConditionJson = GetWorkflowConditionJson(viewModel.Task_ConditionTaskType, viewModel.Task_ConditionLeadStatus);


            if (viewModel.Action_ChangeLeadStatus)
            {
                workflowToUpdate.Actions.Add(CreateChangeLeadStatusAction(viewModel.ChangeLeadStatus_NewStatusId, sharedConditionJson));
            }

            if (viewModel.Action_ChangeLifeCycleStage)
            {
                workflowToUpdate.Actions.Add(CreateChangeLifeCycleStageAction(viewModel.ChangeLifeCycleStage_NewStageId, sharedConditionJson));
            }

            if (viewModel.Action_CreateTask)
            {
                workflowToUpdate.Actions.Add(CreateCreateTaskAction(
                    viewModel.Task_Title,
                    viewModel.Task_DaysDue,
                    viewModel.Task_TaskType,
                    viewModel.Task_PriorityId,
                    viewModel.Task_StatusId,
                    sharedConditionJson));
            }

            _context.Workflows.Update(workflowToUpdate);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Workflow updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        private string GetWorkflowConditionJson(string conditionTaskType, int? conditionLeadStatus)
        {
            var conditionDict = new Dictionary<string, string>();

            if (!string.IsNullOrWhiteSpace(conditionTaskType))
            {
                conditionDict["TaskType"] = conditionTaskType;
            }
            if (conditionLeadStatus.HasValue)
            {
                conditionDict["LeadStatusId"] = conditionLeadStatus.Value.ToString();
            }

            return conditionDict.Count > 0
                ? JsonConvert.SerializeObject(conditionDict)
                : "{}";
        }


        private WorkflowAction CreateChangeLeadStatusAction(LeadStatusEnum newStatus, string conditionJson)
        {
            var parameters = new { NewLeadStatus = newStatus };
            return new WorkflowAction
            {
                ActionType = WorkflowActionType.ChangeLeadStatus,
                ParametersJson = JsonConvert.SerializeObject(parameters),
                ConditionJson = conditionJson 
            };
        }

        private WorkflowAction CreateChangeLifeCycleStageAction(int newStageId, string conditionJson)
        {
            var parameters = new { NewStageId = newStageId };
            return new WorkflowAction
            {
                ActionType = WorkflowActionType.ChangeLifeCycleStage,
                ParametersJson = JsonConvert.SerializeObject(parameters),
                ConditionJson = conditionJson 
            };
        }

        private WorkflowAction CreateCreateTaskAction(
            string title, int daysDue, string taskType, int priorityId, int statusId,
            string conditionJson)
        {
            var parameters = new
            {
                Title = title,
                DaysDue = daysDue,
                TaskType = taskType,
                AssignedTo = "ContactOwner",
                PriorityId = priorityId,
                StatusId = statusId
            };

            return new WorkflowAction
            {
                ActionType = WorkflowActionType.CreateTask,
                ParametersJson = JsonConvert.SerializeObject(parameters),
                ConditionJson = conditionJson
            };
        }


        [Authorize(Roles = "SalesAdminSuper,SalesAdmin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var userId = _currentUserService.GetUserId();
            var organization = await _organizationService.GetOrganizationViewModelByUserId(userId);
            if (organization == null) return Unauthorized();

            var workflow = await _context.Workflows.FirstOrDefaultAsync(
                w => w.Id == id && w.OrganizationId == organization.OrganizationId);

            if (workflow == null) return NotFound();

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
            if (organization == null) return Unauthorized();

            var workflow = await _context.Workflows
                .Include(w => w.Actions)
                .FirstOrDefaultAsync(w => w.Id == id && w.OrganizationId == organization.OrganizationId);

            if (workflow == null) return NotFound();

            _context.Workflows.Remove(workflow);
            await _context.SaveChangesAsync();

            TempData["Message"] = $"Workflow '{workflow.Name}' has been deleted.";
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

        private WorkflowAction CreateChangeLeadStatusAction(LeadStatusEnum newStatus)
        {
            var parameters = new { NewLeadStatus = newStatus };
            return new WorkflowAction
            {
                ActionType = WorkflowActionType.ChangeLeadStatus,
                ParametersJson = JsonConvert.SerializeObject(parameters),
                ConditionJson = "{}"
            };
        }

        private WorkflowAction CreateChangeLifeCycleStageAction(int newStageId)
        {
            var parameters = new { NewStageId = newStageId };
            return new WorkflowAction
            {
                ActionType = WorkflowActionType.ChangeLifeCycleStage,
                ParametersJson = JsonConvert.SerializeObject(parameters),
                ConditionJson = "{}"
            };
        }

        private WorkflowAction CreateCreateTaskAction(
            string title, int daysDue, string taskType, int priorityId, int statusId,
            string conditionTaskType, LeadStatusEnum? conditionLeadStatus)
        {
            var parameters = new
            {
                Title = title,
                DaysDue = daysDue,
                TaskType = taskType,
                AssignedTo = "ContactOwner",
                PriorityId = priorityId,
                StatusId = statusId
            };

            var conditionDict = new Dictionary<string, string>();
            if (!string.IsNullOrWhiteSpace(conditionTaskType))
            {
                conditionDict["TaskType"] = conditionTaskType;
            }
            if (conditionLeadStatus.HasValue)
            {
                conditionDict["LeadStatusId"] = ((int)conditionLeadStatus.Value).ToString();
            }

            return new WorkflowAction
            {
                ActionType = WorkflowActionType.CreateTask,
                ParametersJson = JsonConvert.SerializeObject(parameters),
                ConditionJson = conditionDict.Count > 0
                    ? JsonConvert.SerializeObject(conditionDict)
                    : "{}"
            };
        }

        private class TaskActionParams
        {
            public string Title { get; set; }
            public int DaysDue { get; set; }
            public string TaskType { get; set; }
            public int PriorityId { get; set; }
            public int StatusId { get; set; }
        }
        private class LeadStatusActionParams
        {
            public LeadStatusEnum NewLeadStatus { get; set; }
        }
        private class LifeCycleStageActionParams
        {
            public int NewStageId { get; set; }
        }
    }
}