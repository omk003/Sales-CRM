using scrm_dev_mvc.Models.ViewModels;
using System.Threading.Tasks;

namespace scrm_dev_mvc.services
{
    public interface IWorkspaceService
    {
        Task<WorkspaceViewModel> GetWorkspaceDataAsync(Guid userId, bool isAdmin);
    }
}