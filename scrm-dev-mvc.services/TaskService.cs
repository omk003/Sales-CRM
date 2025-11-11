using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using scrm_dev_mvc.Data.Repository.IRepository;
using scrm_dev_mvc.Models;
using scrm_dev_mvc.Models.DTO; // <-- Import your DTO namespace
using scrm_dev_mvc.Models.Enums;
using scrm_dev_mvc.Models.ViewModels;
using scrm_dev_mvc.services.Interfaces;
using System.Diagnostics;
using LeadStatusEnum = scrm_dev_mvc.Models.Enums.LeadStatusEnum;
namespace scrm_dev_mvc.services
{
    public class TaskService : ITaskService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IActivityService _activityService; 
        private readonly ILogger<TaskService> _logger;
        private readonly IAuditService _auditService; 
        private readonly IWorkflowService _workflowService;

        public TaskService(IUnitOfWork unitOfWork,
                           IActivityService activityService, 
                           ILogger<TaskService> logger, IAuditService auditService, IWorkflowService workflowService)
        {
            _unitOfWork = unitOfWork;
            _activityService = activityService; 
            _logger = logger;
            _auditService = auditService; 
            _workflowService = workflowService;

        }

       
        public async Task<TaskCreateViewModel> GetTaskCreateViewModelAsync(int? contactId, int? companyId, int? dealId)
        {
            // ... (same as before)
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

        // --- THIS METHOD IS NOW UPDATED ---
        public async Task<(bool Success, string Message)> CreateTaskAsync(TaskCreateViewModel viewModel, Guid ownerId)
        {
            Models.Task task; // Use full namespace
            var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Id == ownerId);
            var organizationId = user?.OrganizationId ?? 0;
            // --- Step 1: Create and Save the Task ---
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
                await _unitOfWork.SaveChangesAsync(); // Commit the task
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in CreateTaskAsync (Step 1: Saving Task).");
                return (false, "A database error occurred while creating the task.");
            }

            // --- Step 2: Call your ActivityService to log the activity ---
            try
            {
                var status = await _unitOfWork.TaskStatuses.FirstOrDefaultAsync(u => u.Id == viewModel.StatusId);
                var statusName = status?.StatusName ?? "Pending";

                // Build the DTO your service expects
                var activityDto = new CreateActivityDto
                {
                    ActivityTypeName = "Task",
                    Notes = $"New task created: '{task.Title}'",
                    Status = statusName,
                    DueDate = task.DueDate,
                    ActivityDate = DateTime.UtcNow,
                    ContactId = viewModel.ContactId,
                    DealId = viewModel.DealId,
                    OwnerId = ownerId, // Pass the OwnerId from the controller

                    SubjectId = task.Id, // The ID of the task we just created
                    SubjectType = "Task"
                };

                // This call saves itself (based on your service's code)
                await _activityService.CreateActivityAsync(activityDto);

                return (true, "Task created successfully!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Task (ID: {TaskId}) was created, but failed to create activity log.", task.Id);
                // This is a "compensating transaction" scenario. The task exists, but the activity failed.
                return (false, "Task was created, but logging the activity failed. Please check logs.");
            }
        }


        public async Task<TaskUpdateViewModel> GetTaskUpdateViewModelAsync(int taskId)
        {
            var task = await _unitOfWork.Tasks.FirstOrDefaultAsync(u => u.Id == taskId,
                include: "Contact,Deal"); // Include related data

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
                // Populate all dropdowns
                TaskStatuses = (await _unitOfWork.TaskStatuses.GetAllAsync())
                                .Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.StatusName }),
                LeadStatuses = (await _unitOfWork.LeadStatuses.GetAllAsync())
                                .Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.LeadStatusName }),
                DealStages = (await _unitOfWork.Stages.GetAllAsync())
                                .Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Name }),
            };

            // If linked to a Contact, pre-fill its current status
            if (task.Contact != null)
            {
                viewModel.ContactName = task.Contact.FirstName ?? task.Contact.Email; // Assuming Contact has a Name property
                viewModel.CurrentContactLeadStatusId = task.Contact.LeadStatusId; // Assuming this property exists
                viewModel.ContactEmail = task.Contact.Email;
                viewModel.ContactPhoneNumber = task.Contact.Number;
            }

            // If linked to a Deal, pre-fill its current stage
            if (task.Deal != null)
            {
                viewModel.DealName = task.Deal.Name;
                viewModel.CurrentDealStageId = task.Deal.StageId; // Assuming this property exists
            }

            return viewModel;
        }


        //public async Task<(bool Success, string Message)> UpdateTaskAndEntitiesAsync(
        //TaskUpdateViewModel viewModel, Guid ownerId) 
        //{
        //    try
        //    {
        //        var task = await _unitOfWork.Tasks.FirstOrDefaultAsync(u=>u.Id == viewModel.TaskId);
        //        if (task == null) return (false, "Task not found.");

        //        // --- 1. AUDIT LOGIC: Compare Task fields ---
        //        if (task.StatusId != viewModel.StatusId)
        //        {
        //            await _auditService.LogChangeAsync(new AuditLogDto
        //            {
        //                OwnerId = ownerId,
        //                RecordId = task.Id,
        //                TableName = "Task",
        //                FieldName = "StatusId",
        //                OldValue = task.StatusId.ToString(),
        //                NewValue = viewModel.StatusId.ToString()
        //            });
        //            task.StatusId = viewModel.StatusId; // Apply change

        //            // Check if new status is "Completed"
        //            var completedStatus = await _unitOfWork.TaskStatuses.FirstOrDefaultAsync(s => s.StatusName == "Completed");
        //            if (completedStatus != null && task.StatusId == completedStatus.Id)
        //            {
        //                task.CompletedAt = DateTime.UtcNow;

        //                // --- FIRE THE TRIGGER ---
        //                await _workflowService.RunTriggersAsync(
        //                    WorkflowTrigger.TaskCompleted,
        //                    task // Pass the completed task object
        //                );
        //                // ------------------------
        //            }
        //        }
        //        _unitOfWork.Tasks.Update(task);

        //        // --- 2. AUDIT LOGIC: Compare Contact fields ---
        //        if (viewModel.ContactId.HasValue && viewModel.CurrentContactLeadStatusId.HasValue)
        //        {
        //            var contact = await _unitOfWork.Contacts.FirstOrDefaultAsync(u => u.Id == viewModel.ContactId.Value);
        //            int newStatusId = viewModel.CurrentContactLeadStatusId.Value;

        //            if (contact != null && contact.LeadStatusId != viewModel.CurrentContactLeadStatusId.Value)
        //            {
        //                await _auditService.LogChangeAsync(new AuditLogDto
        //                {
        //                    OwnerId = ownerId,
        //                    RecordId = contact.Id,
        //                    TableName = "Contact",
        //                    FieldName = "LeadStatusId",
        //                    OldValue = contact.LeadStatusId.ToString(),
        //                    NewValue = viewModel.CurrentContactLeadStatusId.Value.ToString()
        //                });
        //                contact.LeadStatusId = newStatusId;//changed
        //                // --- FIRE THE TRIGGER ---
        //                // Convert the int ID to the correct trigger
        //                if (TryGetTriggerForStatus(newStatusId, out WorkflowTrigger trigger))
        //                {
        //                    await _workflowService.RunTriggersAsync(trigger, contact);
        //                }
        //                // ------------------------

        //                _unitOfWork.Contacts.Update(contact);
        //            }
        //        }

        //        // --- 3. AUDIT LOGIC: Compare Deal fields ---
        //        if (viewModel.DealId.HasValue && viewModel.CurrentDealStageId.HasValue)
        //        {
        //            var deal = await _unitOfWork.Deals.FirstOrDefaultAsync(u => u.Id == viewModel.DealId.Value);
        //            if (deal != null && deal.StageId != viewModel.CurrentDealStageId.Value)
        //            {
        //                await _auditService.LogChangeAsync(new AuditLogDto
        //                {
        //                    OwnerId = ownerId,
        //                    RecordId = deal.Id,
        //                    TableName = "Deal",
        //                    FieldName = "StageId",
        //                    OldValue = deal.StageId.ToString(),
        //                    NewValue = viewModel.CurrentDealStageId.Value.ToString()
        //                });
        //                deal.StageId = viewModel.CurrentDealStageId.Value; // Apply change
        //                _unitOfWork.Deals.Update(deal);
        //            }
        //        }

        //        // 4. Save all changes (Task, Contact, Deal, AND Audit logs) in one transaction
        //        await _unitOfWork.SaveChangesAsync();
        //        return (true, "Task updated successfully!");
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error updating task and entities.");
        //        return (false, "A database error occurred.");
        //    }
        //}


        public async Task<(bool Success, string Message)> UpdateTaskAndEntitiesAsync(
    TaskUpdateViewModel viewModel, Guid ownerId)
        {
            try
            {
                // 1. Get the task AND its related contact
                // We include "Contact" so we use the same object for all operations
                var task = await _unitOfWork.Tasks.FirstOrDefaultAsync(u => u.Id == viewModel.TaskId, include: "Contact");
                if (task == null) return (false, "Task not found.");

                bool taskWasCompleted = false;
                bool leadStatusWasManuallyChanged = false;

                // --- 1. Handle MANUAL Lead Status change FIRST ---
                // This block runs if the user *manually* changed the Lead Status dropdown in the modal.
                if (viewModel.ContactId.HasValue && viewModel.CurrentContactLeadStatusId.HasValue)
                {
                    var contact = task.Contact; // Use the already-loaded contact
                    if (contact == null)
                    {
                        contact = await _unitOfWork.Contacts.FirstOrDefaultAsync(u => u.Id == viewModel.ContactId.Value);
                    }

                    int newManualStatusId = viewModel.CurrentContactLeadStatusId.Value;

                    if (contact != null && contact.LeadStatusId != newManualStatusId)
                    {
                        await _auditService.LogChangeAsync(new AuditLogDto
                        {
                            OwnerId = ownerId,
                            RecordId = contact.Id,
                            TableName = "Contact",
                            FieldName = "LeadStatusId",
                            OldValue = contact.LeadStatusId.ToString(),
                            NewValue = newManualStatusId.ToString()
                        });

                        contact.LeadStatusId = newManualStatusId;
                        leadStatusWasManuallyChanged = true; // Flag that this happened

                        // Fire the trigger for the *manual* change
                        if (TryGetTriggerForStatus(newManualStatusId, out WorkflowTrigger trigger))
                        {
                            await _workflowService.RunTriggersAsync(trigger, contact);
                        }
                        _unitOfWork.Contacts.Update(contact);
                    }
                }

                // --- 2. Handle Task Status change ---
                if (task.StatusId != viewModel.StatusId)
                {
                    await _auditService.LogChangeAsync(new AuditLogDto
                    {
                        OwnerId = ownerId,
                        RecordId = task.Id,
                        TableName = "Task",
                        FieldName = "StatusId",
                        OldValue = task.StatusId.ToString(),
                        NewValue = viewModel.StatusId.ToString()
                    });

                    task.StatusId = viewModel.StatusId; // Apply change

                    var completedStatus = await _unitOfWork.TaskStatuses.FirstOrDefaultAsync(s => s.StatusName == "Completed");
                    if (completedStatus != null && task.StatusId == completedStatus.Id)
                    {
                        task.CompletedAt = DateTime.UtcNow;
                        taskWasCompleted = true; // Flag that this happened
                    }
                    _unitOfWork.Tasks.Update(task);
                }

                // --- 3. Handle Deal change ---
                if (viewModel.DealId.HasValue && viewModel.CurrentDealStageId.HasValue)
                {
                    // ... (Your existing deal logic here) ...
                    // var deal = ...
                    // _unitOfWork.Deals.Update(deal);
                }

                // --- 4. Save ALL manual changes in one transaction ---
                await _unitOfWork.SaveChangesAsync();

                // --- 5. Fire the AUTOMATIC TaskCompleted trigger ---
                // ONLY run this workflow if the task was completed AND
                // the user did NOT *also* manually change the lead status.
                // This gives manual changes priority and prevents the fight.
                if (taskWasCompleted && !leadStatusWasManuallyChanged)
                {
                    // This runs in its own transaction (Context_B) *after*
                    // Context_A is finished, so it cannot be overwritten.
                    await _workflowService.RunTriggersAsync(
                        WorkflowTrigger.TaskCompleted,
                        task // Pass the completed task object
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
            // Cast the int ID to your LeadStatus enum
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
    }
}