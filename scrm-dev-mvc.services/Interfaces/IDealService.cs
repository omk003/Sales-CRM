using scrm_dev_mvc.Models;
using scrm_dev_mvc.Models.ViewModels;

namespace scrm_dev_mvc.services.Interfaces
{
    public interface IDealService
    {
        Task<KanbanBoardViewModel> GetKanbanBoardAsync(Guid ownerId);
        Task<DealFormViewModel> GetCreateFormAsync(int? companyId);
        Task<bool> InsertDealAsync(DealFormViewModel vm, Guid userId);
        Task<DealFormViewModel?> GetUpdateFormAsync(int id);
        Task<bool> UpdateDealAsync(DealFormViewModel vm);
        Task<DealPreviewViewModel?> GetDealDetailsAsync(int id);
        Task<IEnumerable<object>> GetAllDealsForUserAsync(Guid userId);
        Task<bool> UpdateDealStageAsync(int dealId, string newStageName);

        Task<bool> DeleteDealAsync(int dealId);
        Task<bool> AssociateCompanyAsync(int dealId, int companyId);

        Task<bool> DisassociateCompanyAsync(int dealId);
    }
}
