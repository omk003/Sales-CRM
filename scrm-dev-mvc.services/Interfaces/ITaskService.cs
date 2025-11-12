using scrm_dev_mvc.Models.ViewModels;

namespace scrm_dev_mvc.services.Interfaces
{
    public interface ITaskService
    {
        Task<TaskCreateViewModel> GetTaskCreateViewModelAsync(int? contactId, int? companyId, int? dealId);
        Task<(bool Success, string Message)> CreateTaskAsync(TaskCreateViewModel viewModel, Guid ownerId);

        Task<TaskUpdateViewModel> GetTaskUpdateViewModelAsync(int taskId);
        Task<(bool Success, string Message)> UpdateTaskAndEntitiesAsync(TaskUpdateViewModel viewModel, Guid ownerId);
        Task<(bool Success, string Message)> DeleteTaskAsync(int taskId, Guid ownerId);
    }
}