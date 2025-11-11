using scrm_dev_mvc.Models;
using scrm_dev_mvc.Models.ViewModels;

namespace scrm_dev_mvc.services.Interfaces
{
    public interface ICompanyService
    {
        Task<string> CreateCompanyAsync(CompanyViewModel companyViewModel);

        Task<List<CompanyViewModel>> GetAllCompany(Guid userId);
        Task<bool> DeleteCompanyByIdsAsync(List<int> ids);
        Task<string> UpdateCompany(CompanyViewModel company);
        Task<Company?> GetCompanyForPreviewAsync(int id);
        Company GetCompanyById(int id);
    }
}