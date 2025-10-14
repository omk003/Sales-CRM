﻿using scrm_dev_mvc.Models;
using scrm_dev_mvc.Models.ViewModels;

namespace scrm_dev_mvc.services
{
    public interface ICompanyService
    {
        Task<string> CreateCompanyAsync(CompanyViewModel companyViewModel);

        Task<List<CompanyViewModel>> GetAllCompany(Guid userId);
        Task<bool> DeleteCompanyByIdsAsync(List<int> ids);
        Task<string> UpdateCompany(CompanyViewModel company);

        Company GetCompanyById(int id);
    }
}