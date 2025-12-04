using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using scrm_dev_mvc.Data.Repository.IRepository;
using scrm_dev_mvc.DataAccess.Data;
using scrm_dev_mvc.Models;
using scrm_dev_mvc.Models.Enums;
using scrm_dev_mvc.Models.ViewModels;
using scrm_dev_mvc.services.Interfaces;
using scrm_dev_mvc.Services.WorkflowActions;

namespace scrm_dev_mvc.Services
{
    public class WorkflowService : IWorkflowService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<WorkflowService> _logger;
        private readonly IServiceProvider _serviceProvider;

        private readonly Dictionary<WorkflowActionType, IWorkflowActionExecutor> _actionExecutors;
         
        public WorkflowService(
            ApplicationDbContext context,
            ILogger<WorkflowService> logger,
            IServiceProvider serviceProvider,
            IEnumerable<IWorkflowActionExecutor> actionExecutors)
        {
            _context = context;
            _logger = logger;
            _serviceProvider = serviceProvider;

            _actionExecutors = actionExecutors.ToDictionary(e => e.ActionType);
        }


        public async System.Threading.Tasks.Task RunTriggersAsync(WorkflowTrigger trigger, object entity)
        {
            if (entity is Models.Task task && task.ContactId.HasValue && task.Contact == null)
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    task.Contact = await dbContext.Contacts.AsNoTracking().FirstOrDefaultAsync(c => c.Id == task.ContactId.Value);
                }
            }

            if (!TryGetOrganizationId(entity, out int organizationId))
            {
                _logger.LogWarning("Could not determine OrganizationId for trigger {Trigger}.", trigger);
                return;
            }

            var jsonSettings = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                PreserveReferencesHandling = PreserveReferencesHandling.Objects
            };

            string serializedEntity = JsonConvert.SerializeObject(entity, jsonSettings);
            object entitySnapshot = JsonConvert.DeserializeObject(serializedEntity, entity.GetType(), jsonSettings);


            var workflows = await _context.Workflows
                .Include(wf => wf.Actions)
                .Where(wf => wf.Event == trigger &&
                                 wf.OrganizationId == organizationId &&
                                 wf.IsActive)
                .ToListAsync();

            if (!workflows.Any()) return;

            _logger.LogInformation("Found {Count} workflows for trigger {Trigger}.", workflows.Count, trigger);

            foreach (var workflow in workflows)
            {
                foreach (var action in workflow.Actions)
                {
                    try
                    {
                        if (!MatchesCondition(action.ConditionJson, entitySnapshot))
                        {
                            _logger.LogInformation($"Action {action.Id} skipped due to condition mismatch.");
                            continue;
                        }

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
            if (_actionExecutors.TryGetValue(action.ActionType, out var executor))
            {
                await executor.ExecuteAsync(action, entity);
            }
            else
            {
                _logger.LogWarning("Unknown workflow action type: {ActionType}", action.ActionType);
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
                    if (value == null) return false;

                    var type = value.GetType();
                    var prop = type.GetProperty(key);

                    if (prop == null && value is Models.Task task && task.Contact != null)
                    {
                        var contactProp = typeof(Contact).GetProperty(key);
                        if (contactProp != null)
                        {
                            value = task.Contact;
                            prop = contactProp;
                        }
                    }

                    if (prop == null) return false;

                    value = prop.GetValue(value);
                }

                string entityValue = value?.ToString();
                string ruleValue = kvp.Value?.ToString();

                if (entityValue != ruleValue)
                    return false;
            }
            return true;
        }
    }
}