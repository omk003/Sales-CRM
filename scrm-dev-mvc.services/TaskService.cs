using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using scrm_dev_mvc.Data.Repository.IRepository;
using scrm_dev_mvc.Models;
using scrm_dev_mvc.Models.DTO; // <-- Import your DTO namespace
using scrm_dev_mvc.Models.ViewModels;
using scrm_dev_mvc.services;
using scrm_dev_mvc.Services;
using System.Diagnostics;

namespace scrm_dev_mvc.services
{
    public class TaskService : ITaskService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IActivityService _activityService; // <-- INJECT THIS
        private readonly ILogger<TaskService> _logger;

        public TaskService(IUnitOfWork unitOfWork,
                           IActivityService activityService, // <-- ADD THIS
                           ILogger<TaskService> logger)
        {
            _unitOfWork = unitOfWork;
            _activityService = activityService; // <-- ADD THIS
            _logger = logger;
        }

        // No changes to this method
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

            // --- Step 1: Create and Save the Task ---
            try
            {
                task = new Models.Task
                {
                    Title = viewModel.Title,
                    Description = viewModel.Description,
                    PriorityId = viewModel.PriorityId,
                    StatusId = viewModel.StatusId,
                    DueDate = viewModel.DueDate,
                    ContactId = viewModel.ContactId,
                    CompanyId = viewModel.CompanyId,
                    DealId = viewModel.DealId
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
            }

            // If linked to a Deal, pre-fill its current stage
            if (task.Deal != null)
            {
                viewModel.DealName = task.Deal.Name;
                viewModel.CurrentDealStageId = task.Deal.StageId; // Assuming this property exists
            }

            return viewModel;
        }

        // --- NEW METHOD: POST ---
        public async Task<(bool Success, string Message)> UpdateTaskAndEntitiesAsync(TaskUpdateViewModel viewModel)
        {
            try
            {
                var task = await _unitOfWork.Tasks.FirstOrDefaultAsync(u => u.Id == viewModel.TaskId, include:"Status");
                if (task == null) return (false, "Task not found.");

                // 1. Update the Task itself
                task.Title = viewModel.Title;
                task.Description = viewModel.Description;
                task.DueDate = viewModel.DueDate;

                // Check if status changed
                if (task.StatusId != viewModel.StatusId)
                {
                    task.StatusId = viewModel.StatusId;
                    // If new status is "Completed", mark it
                    var completedStatus = await _unitOfWork.TaskStatuses.FirstOrDefaultAsync(s => s.StatusName == "Completed");
                    if (completedStatus != null && task.StatusId == completedStatus.Id)
                    {
                        task.CompletedAt = DateTime.UtcNow;
                    }
                }
                _unitOfWork.Tasks.Update(task);

                // 2. Update the related Contact, if any
                if (viewModel.ContactId.HasValue && viewModel.CurrentContactLeadStatusId.HasValue)
                {
                    var contact = await _unitOfWork.Contacts.FirstOrDefaultAsync(u=>u.Id == viewModel.ContactId.Value);
                    if (contact != null)
                    {
                        contact.LeadStatusId = viewModel.CurrentContactLeadStatusId.Value;
                        _unitOfWork.Contacts.Update(contact);
                    }
                }

                // 3. Update the related Deal, if any
                if (viewModel.DealId.HasValue && viewModel.CurrentDealStageId.HasValue)
                {
                    var deal = await _unitOfWork.Deals.FirstOrDefaultAsync(u => u.Id == viewModel.DealId.Value);
                    if (deal != null)
                    {
                        deal.StageId = viewModel.CurrentDealStageId.Value;
                        _unitOfWork.Deals.Update(deal);
                    }
                }
                var activity = await _unitOfWork.Activities.FirstOrDefaultAsync(u => u.SubjectId == task.Id);

                activity.Status = task.Status.StatusName;

                // 4. Save all changes in one transaction
                await _unitOfWork.SaveChangesAsync();
                return (true, "Task updated successfully!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating task and entities.");
                return (false, "A database error occurred.");
            }
        }
    }
}