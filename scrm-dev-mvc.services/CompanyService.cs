using Microsoft.Extensions.Logging;
using scrm_dev_mvc.Data.Repository;
using scrm_dev_mvc.Data.Repository.IRepository;
using scrm_dev_mvc.Models;
using scrm_dev_mvc.Models.ViewModels;
using scrm_dev_mvc.services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace scrm_dev_mvc.services
{
    public class CompanyService(IUnitOfWork unitOfWork,ILogger<CompanyService> logger) : ICompanyService
    {
        public async Task<string> CreateCompanyAsync(CompanyViewModel companyViewModel)
        {
            if (companyViewModel == null)
            {
                return null;
            }
            if (string.IsNullOrEmpty(companyViewModel.Domain))
            {
                return null;
            }
            var user = await unitOfWork.Users.FirstOrDefaultAsync(u => u.Id == companyViewModel.userId);
            var existingCompany = await unitOfWork.Company.FirstOrDefaultAsync(c => c.Domain == companyViewModel.Domain && c.OrganizationId == user.OrganizationId);
            if (existingCompany != null)
            {
                if (existingCompany.IsDeleted == true)
                {
                    existingCompany.IsDeleted = false;
                    existingCompany.Name = companyViewModel.Name;
                    existingCompany.City = companyViewModel.City;
                    existingCompany.Country = companyViewModel.Country;
                    existingCompany.CreatedAt = companyViewModel.CreatedDate;
                    existingCompany.UserId = companyViewModel.userId;
                    existingCompany.Domain = companyViewModel.Domain;
                    unitOfWork.Company.Update(existingCompany);
                    await unitOfWork.SaveChangesAsync();
                    return "Company created successfully";
                }
                else
                {
                    return "Company with this email already exists";
                }

            }
            var Company = new Company
            {
                Name = companyViewModel.Name,
                City = companyViewModel.City,
                Country = companyViewModel.Country,
                CreatedAt = companyViewModel.CreatedDate,
                Domain = companyViewModel.Domain,
                UserId = companyViewModel.userId,
                OrganizationId = user?.OrganizationId ?? 0
            };
            await unitOfWork.Company.AddAsync(Company);
            await unitOfWork.SaveChangesAsync();
            return "Company created successfully";
        }

        public async Task<List<CompanyViewModel>> GetAllCompany(Guid userId)
        {

            var user = await unitOfWork.Users.GetByIdAsync(userId);
            if (user == null)
                return new List<CompanyViewModel>();

            if (user.RoleId == 2 || user.RoleId == 3)
            {
                var organizationId = user.OrganizationId;
                var CompanyList = await unitOfWork.Company.GetAllAsync(c => c.OrganizationId == organizationId && c.IsDeleted == false);

                List<CompanyViewModel> CompanyResponseViewModels = new List<CompanyViewModel>();
                foreach (var company in CompanyList)
                {
                    var id = company.Id;
                    var currentUser = await unitOfWork.Users.GetByIdAsync(company.UserId ?? Guid.Empty);
                    CompanyResponseViewModels.Add(new CompanyViewModel
                    {
                        Id = id,
                        Name = company.Name,
                        City = company.City,
                        Country = company.Country,
                        userName = currentUser.FirstName,
                        CreatedDate = company.CreatedAt,
                    });
                }
                return CompanyResponseViewModels;

            }
            var CompanyObject = await unitOfWork.Company.GetAllAsync(c => c.OrganizationId == user.OrganizationId && c.UserId == user.Id);
           
            List<CompanyViewModel> CompanyResponseViewModelsObject = new List<CompanyViewModel>();
            foreach (var company in CompanyObject)
            {
                var id = company.Id;
                var currentUser = await unitOfWork.Users.GetByIdAsync(company.UserId ?? Guid.Empty);
                CompanyResponseViewModelsObject.Add(new CompanyViewModel
                {
                    Id = id,
                    Name = company.Name,
                    City = company.City,
                    Country = company.Country,
                    userName = currentUser.FirstName,
                    CreatedDate = company.CreatedAt,
                });
            }
            return CompanyResponseViewModelsObject;

        }

        public async Task<bool> DeleteCompanyByIdsAsync(List<int> ids)
        {
            foreach (var id in ids)
            {
                var Company = await unitOfWork.Company.FirstOrDefaultAsync(u => u.Id == id);
                if (Company != null)
                {
                    Company.IsDeleted = true;
                    unitOfWork.Company.Update(Company);
                }
            }
            await unitOfWork.SaveChangesAsync();
            return true;

        }


        public async Task<string> UpdateCompany(CompanyViewModel company)
        {
            if (company != null)
            {
                Company existingCompany = unitOfWork.Company.FirstOrDefaultAsync(c => c.Id == company.Id).Result;
                if (existingCompany != null)
                {
                   
                    existingCompany.Name = company.Name;
                    existingCompany.City = company.City;
                    existingCompany.Country = company.Country;
                    existingCompany.CreatedAt = company.CreatedDate;
                    existingCompany.UserId = company.userId;
                    existingCompany.Domain = company.Domain;
                    unitOfWork.Company.Update(existingCompany);
                    await unitOfWork.SaveChangesAsync();
                    return "Company updated successfully";
                }
            }
            return "Company updation Failed";
        }
        public async Task<Company?> GetCompanyForPreviewAsync(int id, Guid userId)
        {   
            try
            {
                
                string includeProperties =
                    "Deals," +
                    "Contacts," +
                    "Contacts.Activities," +
                    "Contacts.Activities.ActivityType,"+ "Contacts.Activities.Owner";

                var company = await unitOfWork.Company.FirstOrDefaultAsync(
                    predicate: c => c.Id == id,
                    include: includeProperties
                );
                if(userId != Guid.Empty && company != null)
                {
                    var user =  unitOfWork.Users.GetByIdAsync(userId).Result;
                    if(user.RoleId == 2 || user.RoleId ==3)
                    {
                        if(company.OrganizationId != user.OrganizationId)
                        {
                            return null;
                        }
                    }
                    else
                    {
                        if(company.UserId != user.Id)
                        {
                            return null;
                        }
                    }
                }
                return company;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting company for preview with ID {CompanyId}", id);
                return null;
            }
        }
        public Company GetCompanyById(int id,Guid userId)
        {
            var Company = unitOfWork.Company.FirstOrDefaultAsync(c => c.Id == id, "Deals,Contacts").Result;
            if(userId != null && Company != null)
            {
                var user =  unitOfWork.Users.GetByIdAsync(userId).Result;
                if(user.RoleId == 2 || user.RoleId ==3)
                {
                    if(Company.OrganizationId != user.OrganizationId)
                    {
                        return null;
                    }
                }
                else
                {
                    if(Company.UserId != user.Id)
                    {
                        return null;
                    }
                }
            }
            return Company;
        }
    }
}
