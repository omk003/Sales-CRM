using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using scrm_dev_mvc.Data.Repository.IRepository;
using scrm_dev_mvc.Models;
using scrm_dev_mvc.Models.DTO; // <-- Import your DTO namespace
using scrm_dev_mvc.Models.Enums;
using scrm_dev_mvc.Models.ViewModels;
using scrm_dev_mvc.services.Interfaces;
using System.Diagnostics;
using Twilio.TwiML.Voice;
using LeadStatusEnum = scrm_dev_mvc.Models.Enums.LeadStatusEnum;
namespace scrm_dev_mvc.services
{
    public class TaskService : ITaskService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IActivityService _activityService; 
        private readonly ILogger<TaskService> _logger;
        private readonly IWorkflowService _workflowService;

        public TaskService(IUnitOfWork unitOfWork,
                           IActivityService activityService, 
                           ILogger<TaskService> logger, IWorkflowService workflowService)
        {
            _unitOfWork = unitOfWork;
            _activityService = activityService; 
            _logger = logger;
            _workflowService = workflowService;

        }

       
        public async Task<TaskCreateViewModel> GetTaskCreateViewModelAsync(int? contactId, int? companyId, int? dealId)
        {
            var viewModel = new TaskCreateViewModel
            {
                ContactId = contactId,
                CompanyId = companyId,
                DealId = dealId,

                Priorities = (await _unitOfWork.Priorities.GetAllAsync())
                                .Select(p => new SelectListItem { Value = p.Id.ToString(), Text = p.Name }),

                Statuses = (await _unitOfWork.TaskStatuses.GetAllAsync())
                                .Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.StatusName }),

                DueDate = DateTime.Today.AddDays(1)
            };

            return viewModel;
        }

        public async Task<(bool Success, string Message)> CreateTaskAsync(TaskCreateViewModel viewModel, Guid ownerId)
        {
            Models.Task task; 
            var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Id == ownerId);
            var organizationId = user?.OrganizationId ?? 0;
            try
            {
                task = new Models.Task
                {
                    Title = viewModel.Title,
                    Description = viewModel.Description,
                    TaskType = viewModel.TaskType,
                    PriorityId = viewModel.PriorityId,
                    StatusId = viewModel.StatusId,
                    DueDate = viewModel.DueDate,
                    ContactId = viewModel.ContactId,
                    CompanyId = viewModel.CompanyId,
                    DealId = viewModel.DealId,
                    OwnerId = ownerId,
                    OrganizationId = organizationId
                };

                await _unitOfWork.Tasks.AddAsync(task);
                await _unitOfWork.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in CreateTaskAsync (Step 1: Saving Task).");
                return (false, "A database error occurred while creating the task.");
            }

            try
            {
                var status = await _unitOfWork.TaskStatuses.FirstOrDefaultAsync(u => u.Id == viewModel.StatusId);
                var statusName = status?.StatusName ?? "Pending";

                var activityDto = new CreateActivityDto
                {
                    ActivityTypeName = "Task",
                    Notes = $"New task created: '{task.Title}'",
                    Status = statusName,
                    DueDate = task.DueDate,
                    ActivityDate = DateTime.UtcNow,
                    ContactId = viewModel.ContactId,
                    DealId = viewModel.DealId,
                    OwnerId = ownerId,

                    SubjectId = task.Id, 
                    SubjectType = "Task"
                };

                await _activityService.CreateActivityAsync(activityDto);

                return (true, "Task created successfully!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Task (ID: {TaskId}) was created, but failed to create activity log.", task.Id);
                return (false, "Task was created, but logging the activity failed. Please check logs.");
            }
        }


        public async Task<TaskUpdateViewModel> GetTaskUpdateViewModelAsync(int taskId)
        {
            var task = await _unitOfWork.Tasks.FirstOrDefaultAsync(u => u.Id == taskId,
                include: "Contact,Deal"); 

            if (task == null)
                throw new InvalidOperationException("Task not found.");

            var viewModel = new TaskUpdateViewModel
            {
                TaskId = task.Id,
                Title = task.Title,
                Description = task.Description,
                StatusId = task.StatusId,
                DueDate = task.DueDate,
                ContactId = task.ContactId,
                DealId = task.DealId,
                TaskType = task.TaskType,
                TaskStatuses = (await _unitOfWork.TaskStatuses.GetAllAsync())
                                .Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.StatusName }),
                LeadStatuses = (await _unitOfWork.LeadStatuses.GetAllAsync())
                                .Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.LeadStatusName }),
                DealStages = (await _unitOfWork.Stages.GetAllAsync())
                                .Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Name }),
            };

            if (task.Contact != null)
            {
                viewModel.ContactName = task.Contact.FirstName ?? task.Contact.Email; 
                viewModel.CurrentContactLeadStatusId = task.Contact.LeadStatusId; 
                viewModel.ContactEmail = task.Contact.Email;
                viewModel.ContactPhoneNumber = task.Contact.Number;
            }

            if (task.Deal != null)
            {
                viewModel.DealName = task.Deal.Name;
                viewModel.CurrentDealStageId = task.Deal.StageId; 
            }

            return viewModel;
        }



        public async Task<(bool Success, string Message)> UpdateTaskAndEntitiesAsync(
                            TaskUpdateViewModel viewModel, Guid ownerId)
        {
            try
            {
                var task = await _unitOfWork.Tasks.FirstOrDefaultAsync(u => u.Id == viewModel.TaskId, include: "Contact");
                if (task == null) return (false, "Task not found.");

                bool taskWasCompleted = false;
                bool leadStatusWasManuallyChanged = false;

                
                if (viewModel.ContactId.HasValue && viewModel.CurrentContactLeadStatusId.HasValue)
                {
                    var contact = task.Contact;
                    if (contact == null)
                    {
                        contact = await _unitOfWork.Contacts.FirstOrDefaultAsync(u => u.Id == viewModel.ContactId.Value);
                    }

                    int newManualStatusId = viewModel.CurrentContactLeadStatusId.Value;

                    if (contact != null && contact.LeadStatusId != newManualStatusId)
                    {
                        contact.LeadStatusId = newManualStatusId;
                        leadStatusWasManuallyChanged = true;

                        _unitOfWork.Contacts.Update(contact);
                        if (TryGetTriggerForStatus(newManualStatusId, out WorkflowTrigger trigger))
                        {
                            await _workflowService.RunTriggersAsync(trigger, contact);
                        }
                       
                    }
                }

                if (task.StatusId != viewModel.StatusId)
                {

                    task.StatusId = viewModel.StatusId; 
                    var completedStatus = await _unitOfWork.TaskStatuses.FirstOrDefaultAsync(s => s.StatusName == "Completed");
                    if (completedStatus != null && task.StatusId == completedStatus.Id)
                    {
                        task.CompletedAt = DateTime.UtcNow;
                        taskWasCompleted = true; 
                    }
                    _unitOfWork.Tasks.Update(task);
                }

                if (viewModel.DealId.HasValue && viewModel.CurrentDealStageId.HasValue)
                {
                    

                    var deal = await _unitOfWork.Deals.FirstOrDefaultAsync(d => d.Id == viewModel.DealId.Value);

                    if(deal != null)
                    {
                        int oldStageId = deal.StageId;

                        deal.StageId = viewModel.CurrentDealStageId.Value;

                        _unitOfWork.Deals.Update(deal);

                    }
                    
                }


                await _unitOfWork.SaveChangesAsync();

               
                if (taskWasCompleted && !leadStatusWasManuallyChanged)
                {
                    
                    await _workflowService.RunTriggersAsync(
                        WorkflowTrigger.TaskCompleted,
                        task 
                    );
                }

                return (true, "Task updated successfully!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating task and entities.");
                if (ex.Source == "WorkflowService")
                {
                    return (false, "Task updated, but a workflow failed. Check logs.");
                }
                return (false, "A database error occurred.");
            }
        }

        private bool TryGetTriggerForStatus(int statusId, out WorkflowTrigger trigger)
        {
            LeadStatusEnum status = (LeadStatusEnum)statusId;

            switch (status)
            {
                case LeadStatusEnum.Open:
                    trigger = WorkflowTrigger.ContactLeadStatusChangesToOpen;
                    return true;
                case LeadStatusEnum.Connected:
                    trigger = WorkflowTrigger.ContactLeadStatusChangesToConnected;
                    return true;
                case LeadStatusEnum.Qualified:
                    trigger = WorkflowTrigger.ContactLeadStatusChangesToQualified;
                    return true;
                case LeadStatusEnum.Disqualified:
                    trigger = WorkflowTrigger.ContactLeadStatusChangesToDisqualified;
                    return true;
                case LeadStatusEnum.BadTiming:
                    trigger = WorkflowTrigger.ContactLeadStatusChangesToBadTiming;
                    return true;
                case LeadStatusEnum.HighPrice:
                    trigger = WorkflowTrigger.ContactLeadStatusChangesToHighPrice;
                    return true;
                default:
                    trigger = default;
                    return false;
            }
        }


        public async Task<(bool Success, string Message)> DeleteTaskAsync(int taskId, Guid ownerId)
        {
            try
            {
                var task = await _unitOfWork.Tasks.FirstOrDefaultAsync(t => t.Id == taskId);
                if (task == null)
                {
                    return (false, "Task not found.");
                }

                

                var activityDeleted = await _activityService.DeleteActivityBySubjectAsync(taskId, "Task");
                if (!activityDeleted)
                {
                    _logger.LogWarning("Task {TaskId} is being deleted, but its associated activity log could not be marked for deletion.", taskId);
                }

                _unitOfWork.Tasks.Delete(task);

                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Task {TaskId} deleted successfully by user {OwnerId}.", taskId, ownerId);
                return (true, "Task deleted successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting task {TaskId}.", taskId);
                return (false, "An error occurred while deleting the task.");
            }
        }
    }
}