using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using scrm_dev_mvc.DataAccess.Data;
using scrm_dev_mvc.Models;
using scrm_dev_mvc.Models.Enums;
using scrm_dev_mvc.services;
using scrm_dev_mvc.services.Interfaces;

namespace scrm_dev_mvc.Services.WorkflowActions
{
    public class ChangeLifeCycleStageExecutor : IWorkflowActionExecutor
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ChangeLifeCycleStageExecutor> _logger;

        public ChangeLifeCycleStageExecutor(IServiceProvider serviceProvider, ILogger<ChangeLifeCycleStageExecutor> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public WorkflowActionType ActionType => WorkflowActionType.ChangeLifeCycleStage;

        private class ChangeLifeCycleStageParams
        {
            public int NewStageId { get; set; }
        }

        public async System.Threading.Tasks.Task ExecuteAsync(WorkflowAction action, object entity)
        {
            if (!WorkflowServiceHelpers.TryGetContactId(entity, out int contactId))
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
    }
}