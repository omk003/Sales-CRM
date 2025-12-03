using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using scrm_dev_mvc.DataAccess.Data;
using scrm_dev_mvc.Models;
using scrm_dev_mvc.Models.Enums;
using scrm_dev_mvc.Models.ViewModels;
using scrm_dev_mvc.services.Interfaces;

namespace scrm_dev_mvc.Services.WorkflowActions
{
    public class CreateTaskExecutor : IWorkflowActionExecutor
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<CreateTaskExecutor> _logger;

        public CreateTaskExecutor(IServiceProvider serviceProvider, ILogger<CreateTaskExecutor> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public WorkflowActionType ActionType => WorkflowActionType.CreateTask;

        private class CreateTaskParams
        {
            public string Title { get; set; }
            public int DaysDue { get; set; }
            public string TaskType { get; set; }
            public string AssignedTo { get; set; }
            public int PriorityId { get; set; }
            public int StatusId { get; set; }
        }

        public async System.Threading.Tasks.Task ExecuteAsync(WorkflowAction action, object entity)
        {
            Models.Contact contact = null;

            if (entity is Models.Contact c)
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

            Guid assigneeId = contact.OwnerId ?? Guid.Empty;
            if (parameters.AssignedTo == "ContactOwner" && !contact.OwnerId.HasValue)
            {
                _logger.LogWarning("Contact {ContactId} has no OwnerId. Task creation may result in an unassigned task.", contact.Id);
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
    }
}