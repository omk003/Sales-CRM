using scrm_dev_mvc.Models;
using scrm_dev_mvc.Models.Enums;

namespace scrm_dev_mvc.services.Interfaces
{
    public interface IWorkflowActionExecutor
    {
        WorkflowActionType ActionType { get; }
        System.Threading.Tasks.Task ExecuteAsync(WorkflowAction action, object entity);
    }
}