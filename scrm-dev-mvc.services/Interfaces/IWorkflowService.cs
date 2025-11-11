using scrm_dev_mvc.Models.Enums;

namespace scrm_dev_mvc.services.Interfaces
{
    public interface IWorkflowService
    {
        // This is the only public method we need.
        // It takes the trigger (e.g., "ContactCreated")
        // and the entity that caused it (e.g., the new Contact object).
        Task RunTriggersAsync(WorkflowTrigger trigger, object entity);
    }
}