using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json; // Make sure you have Newtonsoft.Json package
using scrm_dev_mvc.Data; // For ApplicationDbContext
using scrm_dev_mvc.DataAccess.Data;
using scrm_dev_mvc.Models;
using scrm_dev_mvc.Models.Enums;
using scrm_dev_mvc.Models.ViewModels;
using scrm_dev_mvc.services.Interfaces;

namespace scrm_dev_mvc.Services
{
    public class WorkflowService : IWorkflowService
    {
        // We'll need the database, a logger, and your TaskService
        private readonly ApplicationDbContext _context;
        private readonly ILogger<WorkflowService> _logger;
        private readonly IServiceProvider _serviceProvider; // Assumes you have this

        public WorkflowService(
            ApplicationDbContext context,
            ILogger<WorkflowService> logger,
            IServiceProvider serviceProvider)
        {
            _context = context;
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        private class UpdateContactParams
        {
            // The enum value for the new status, e.g., 3 (Connected)
            public LeadStatusEnum NewLeadStatus { get; set; }
        }
        // The private DTO class for parsing JSON
        // This makes your `ParametersJson` strongly-typed and easy to use
        private class CreateTaskParams
        {
            public string Title { get; set; }
            public int DaysDue { get; set; }
            public string TaskType { get; set; }
            public string AssignedTo { get; set; } // e.g., "ContactOwner"
            public int PriorityId { get; set; } // NEW
            public int StatusId { get; set; }   // NEW
        }


        private class ChangeLeadStatusParams
        {
            public LeadStatusEnum NewStatus { get; set; }
        }

        private class ChangeLifeCycleStageParams
        {
            public int NewStageId { get; set; } // Assuming it's an int ID
        }

        /// <summary>
        /// The main entry point. Finds and runs all workflows for a given trigger.
        /// </summary>
        public async System.Threading.Tasks.Task RunTriggersAsync(WorkflowTrigger trigger, object entity)
        {
            // 1. Get the OrganizationId from the entity (Contact, Task, etc.)
            if (!TryGetOrganizationId(entity, out int organizationId))
            {
                _logger.LogWarning("Could not determine OrganizationId for trigger {Trigger}.", trigger);
                return;
            }

            // 2. Find all active workflows that match this trigger
            var workflows = await _context.Workflows
                .Include(wf => wf.Actions) // Eager-load the actions
                .Where(wf => wf.Event == trigger &&
                             wf.OrganizationId == organizationId &&
                             wf.IsActive)
                .ToListAsync();

            if (!workflows.Any())
            {
                return; // No workflows for this trigger, which is fine.
            }

            _logger.LogInformation("Found {Count} workflows for trigger {Trigger}.", workflows.Count, trigger);

            // 3. Loop through each workflow and execute its actions
            foreach (var workflow in workflows)
            {
                foreach (var action in workflow.Actions)
                {
                    // Use a try-catch so one failed action doesn't
                    // stop all other workflows.
                    try
                    {
                        await ExecuteActionAsync(action, entity);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Workflow action {ActionId} for workflow {WorkflowId} failed.", action.Id, workflow.Id);
                    }
                }
            }
            }

        /// <summary>
        /// A "router" that calls the correct method for each action type.
        /// </summary>
        /// <summary>
        /// A "router" that calls the correct method for each action type.
        /// </summary>
        private async System.Threading.Tasks.Task ExecuteActionAsync(WorkflowAction action, object entity)
        {
            switch (action.ActionType)
            {
                case WorkflowActionType.CreateTask:
                    await ExecuteCreateTaskActionAsync(action, entity);
                    break;

                // --- UPDATED THIS SECTION ---
                case WorkflowActionType.ChangeLeadStatus:
                    await ExecuteChangeLeadStatusAsync(action, entity);
                    break;
                case WorkflowActionType.ChangeLifeCycleStage:
                    await ExecuteChangeLifeCycleStageAsync(action, entity);
                    break;
                // ---------------------------

                case WorkflowActionType.SendEmail:
                    _logger.LogWarning("SendEmail action not yet implemented.");
                    break;
                default:
                    _logger.LogWarning("Unknown workflow action type: {ActionType}", action.ActionType);
                    break;
            }
        }


        // --- ADD THIS ENTIRE NEW METHOD ---
        /// <summary>
        /// The "worker" for updating a contact's lead status.
        /// </summary>
        private async System.Threading.Tasks.Task ExecuteChangeLeadStatusAsync(WorkflowAction action, object entity)
        {
            if (!TryGetContactId(entity, out int contactId))
            {
                _logger.LogWarning("ChangeLeadStatus action {ActionId} was triggered by an entity with no ContactId. Skipping.", action.Id);
                return;
            }

            var parameters = JsonConvert.DeserializeObject<ChangeLeadStatusParams>(action.ParametersJson);
            if (parameters == null)
            {
                _logger.LogError("Failed to deserialize ParametersJson for Action {ActionId}", action.Id);
                return;
            }

            using (var scope = _serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var targetContact = await dbContext.Contacts.FindAsync(contactId);

                if (targetContact == null)
                {
                    _logger.LogError("Could not find Contact {ContactId} to update for Action {ActionId}", contactId, action.Id);
                    return;
                }

                targetContact.LeadStatusId = (int)parameters.NewStatus;
                dbContext.Contacts.Update(targetContact);
                await dbContext.SaveChangesAsync();

                _logger.LogInformation("Successfully updated Contact {ContactId} LeadStatus to {NewStatus} via Workflow {ActionId}.",
                    targetContact.Id, parameters.NewStatus, action.Id);
            }
        }

        // --- ADDED THIS NEW METHOD (for your LifeCycleStage idea) ---
        private async System.Threading.Tasks.Task ExecuteChangeLifeCycleStageAsync(WorkflowAction action, object entity)
        {
            if (!TryGetContactId(entity, out int contactId))
            {
                _logger.LogWarning("ChangeLifeCycleStage action {ActionId} was triggered by an entity with no ContactId. Skipping.", action.Id);
                return;
            }

            // ... (Logic is the same as above)
            // 1. Deserialize `ChangeLifeCycleStageParams`
            // 2. Find the contact
            // 3. Update `contact.LifeCycleStageId = parameters.NewStageId`
            // 4. SaveChanges
            _logger.LogWarning("ChangeLifeCycleStage action not yet fully implemented.");
        }
        /// <summary>
        /// The "worker" for creating tasks.
        /// </summary>
        // In scrm_dev_mvc/Services/WorkflowService.cs
        // --- ADD THIS NEW HELPER to find ContactId from any entity ---
        private bool TryGetContactId(object entity, out int contactId)
        {
            contactId = 0;
            if (entity is Contact contact)
            {
                contactId = contact.Id;
                return true;
            }
            if (entity is Models.Task task && task.ContactId.HasValue)
            {
                contactId = task.ContactId.Value;
                return true;
            }
            return false;
        }
        private async System.Threading.Tasks.Task ExecuteCreateTaskActionAsync(WorkflowAction action, object entity)
        {
            Contact contact = null; // 1. Define a placeholder for the contact

            // 2. Check what kind of entity triggered the workflow
            if (entity is Contact c)
            {
                // The trigger was 'ContactCreated', so we just use the entity
                contact = c;
            }
            else if (entity is Models.Task task && task.ContactId.HasValue)
            {
                // The trigger was 'TaskCompleted'. We need to find the contact
                // associated with that task.
                // We must do this in a new scope, just like our other methods.
                using (var scope = _serviceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    contact = await dbContext.Contacts.FindAsync(task.ContactId.Value);
                }
            }

            // 3. If we couldn't find a contact, we can't create a task.
            if (contact == null)
            {
                _logger.LogWarning("CreateTask action {ActionId} was triggered but a valid contact could not be found. Skipping.", action.Id);
                return;
            }

            // 4. Deserialize the JSON parameters
            var parameters = JsonConvert.DeserializeObject<CreateTaskParams>(action.ParametersJson);
            if (parameters == null)
            {
                _logger.LogError("Failed to deserialize ParametersJson for Action {ActionId}", action.Id);
                return;
            }

            // 5. Determine the assignee (uses the 'contact' we found)
            Guid assigneeId;
            if (parameters.AssignedTo == "ContactOwner" && contact.OwnerId.HasValue)
            {
                assigneeId = contact.OwnerId.Value;
            }
            else
            {
                assigneeId = contact.OwnerId ?? Guid.Empty;
                if (assigneeId == Guid.Empty)
                {
                    _logger.LogWarning("Contact {ContactId} has no OwnerId. Task creation may fail or be unassigned.", contact.Id);
                }
            }

            // 6. Build the new ViewModel (uses the 'contact' we found)
            var viewModel = new TaskCreateViewModel
            {
                Title = parameters.Title,
                DueDate = DateTime.UtcNow.AddDays(parameters.DaysDue),
                TaskType = parameters.TaskType,
                StatusId = parameters.StatusId,
                PriorityId = parameters.PriorityId,
                ContactId = contact.Id,
                CompanyId = contact.CompanyId // Gets the CompanyId from the contact
            };

            // 7. Use your existing TaskService (in its own scope)
            using (var scope = _serviceProvider.CreateScope())
            {
                var taskService = scope.ServiceProvider.GetRequiredService<ITaskService>();

                var (success, message) = await taskService.CreateTaskAsync(viewModel, assigneeId);

                if (success)
                {
                    _logger.LogInformation("Successfully created task '{Title}' for Contact {ContactId} via Workflow {ActionId}.",
                        viewModel.Title, contact.Id, action.Id);
                }
                else
                {
                    _logger.LogError("TaskService failed to create task for Workflow {ActionId}. Message: {Message}",
                        action.Id, message);
                }
            }
        }

        /// <summary>
        // Helper to get OrganizationId from any entity
        /// </summary>
        private bool TryGetOrganizationId(object entity, out int organizationId)
        {
            organizationId = 0;
            switch (entity)
            {
                case Contact contact:
                    organizationId = contact.OrganizationId;
                    return true;
                case scrm_dev_mvc.Models.Task task:
                    organizationId = task.OrganizationId;
                    return true;
                    // Add other entities here (Company, Deal, etc.)
            }
            return false;
        }
    }
}