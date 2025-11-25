using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json; 
using scrm_dev_mvc.DataAccess.Data;
using scrm_dev_mvc.Models;
using scrm_dev_mvc.Models.Enums;
using scrm_dev_mvc.Models.ViewModels;
using scrm_dev_mvc.services.Interfaces;

namespace scrm_dev_mvc.Services
{
    public class WorkflowService : IWorkflowService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<WorkflowService> _logger;
        private readonly IServiceProvider _serviceProvider; 

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
            public LeadStatusEnum NewLeadStatus { get; set; }
        }
  
        private class CreateTaskParams
        {
            public string Title { get; set; }
            public int DaysDue { get; set; }
            public string TaskType { get; set; }
            public string AssignedTo { get; set; } // e.g., "ContactOwner"
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

        
        public async System.Threading.Tasks.Task RunTriggersAsync(WorkflowTrigger trigger, object entity)
        {
            if (!TryGetOrganizationId(entity, out int organizationId))
            {
                _logger.LogWarning("Could not determine OrganizationId for trigger {Trigger}.", trigger);
                return;
            }

            var workflows = await _context.Workflows
                .Include(wf => wf.Actions) 
                .Where(wf => wf.Event == trigger &&
                             wf.OrganizationId == organizationId &&
                             wf.IsActive)
                .ToListAsync();

            if (!workflows.Any())
            {
                return; 
            }

            _logger.LogInformation("Found {Count} workflows for trigger {Trigger}.", workflows.Count, trigger);

            
            foreach (var workflow in workflows)
            { 
                foreach (var action in workflow.Actions)
                {
                    
                    
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

       
        private async System.Threading.Tasks.Task ExecuteActionAsync(WorkflowAction action, object entity)
        {
            if (!MatchesCondition(action.ConditionJson, entity))
            {
                _logger.LogInformation($"Action {action.Id} skipped due to condition mismatch.");
                return;
            }

            switch (action.ActionType)
            {
                case WorkflowActionType.CreateTask:
                    await ExecuteCreateTaskActionAsync(action, entity);
                    break;

                
                case WorkflowActionType.ChangeLeadStatus:
                    await ExecuteChangeLeadStatusAsync(action, entity);
                    break;
                case WorkflowActionType.ChangeLifeCycleStage:
                    await ExecuteChangeLifeCycleStageAsync(action, entity);
                    break;

                case WorkflowActionType.SendEmail:
                    _logger.LogWarning("SendEmail action not yet implemented.");
                    break;
                default:
                    _logger.LogWarning("Unknown workflow action type: {ActionType}", action.ActionType);
                    break;
            }
        }


        
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

                targetContact.LeadStatusId = (int)parameters.NewLeadStatus;
                dbContext.Contacts.Update(targetContact);
                await dbContext.SaveChangesAsync();

                _logger.LogInformation("Successfully updated Contact {ContactId} LeadStatus to {NewStatus} via Workflow {ActionId}.",
                    targetContact.Id, parameters.NewLeadStatus, action.Id);
            }
        }

        
        private async System.Threading.Tasks.Task ExecuteChangeLifeCycleStageAsync(WorkflowAction action, object entity)
        {
           
            if (!TryGetContactId(entity, out int contactId))
            {
                _logger.LogWarning("ChangeLifeCycleStage action {ActionId} was triggered by an entity with no ContactId. Skipping.", action.Id);
                return;
            }

            
            var parameters = JsonConvert.DeserializeObject<ChangeLifeCycleStageParams>(action.ParametersJson);
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

                
                targetContact.LifeCycleStageId = parameters.NewStageId; 

                
                dbContext.Contacts.Update(targetContact);
                await dbContext.SaveChangesAsync();

                _logger.LogInformation("Successfully updated Contact {ContactId} LifeCycleStage to {NewStageId} via Workflow {ActionId}.",
                  targetContact.Id, parameters.NewStageId, action.Id);
            }
        }
        
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
            Contact contact = null; 

            if (entity is Contact c)
            {
                
                contact = c;
            }
            else if (entity is Models.Task task && task.ContactId.HasValue)
            {
                
                using (var scope = _serviceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    contact = await dbContext.Contacts.FindAsync(task.ContactId.Value);
                }
            }

            
            if (contact == null)
            {
                _logger.LogWarning("CreateTask action {ActionId} was triggered but a valid contact could not be found. Skipping.", action.Id);
                return;
            }

            
            var parameters = JsonConvert.DeserializeObject<CreateTaskParams>(action.ParametersJson);
            if (parameters == null)
            {
                _logger.LogError("Failed to deserialize ParametersJson for Action {ActionId}", action.Id);
                return;
            }

            
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

            
            var viewModel = new TaskCreateViewModel
            {
                Title = parameters.Title,
                DueDate = DateTime.UtcNow.AddDays(parameters.DaysDue),
                TaskType = parameters.TaskType,
                StatusId = parameters.StatusId,
                PriorityId = parameters.PriorityId,
                ContactId = contact.Id,
                CompanyId = contact.CompanyId 
            };

            
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


        private bool MatchesCondition(string conditionJson, object entity)
        {
            if (string.IsNullOrEmpty(conditionJson))
                return true; 

            var conditionDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(conditionJson);

            foreach (var kvp in conditionDict)
            {
                var keys = kvp.Key.Split('.'); 
                object value = entity;

                foreach (var key in keys)
                {
                    var prop = value?.GetType().GetProperty(key);
                    if (prop == null)
                        return false;
                    value = prop.GetValue(value);
                }

                if (value == null || value.ToString() != kvp.Value.ToString())
                    return false;
            }
            return true;
        }
    }
}