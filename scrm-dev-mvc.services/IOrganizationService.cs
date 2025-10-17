using scrm_dev_mvc.Models;
using scrm_dev_mvc.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace scrm_dev_mvc.services
{
    public interface IOrganizationService
    {
        System.Threading.Tasks.Task CreateOrganizationAsync(Organization organization, Guid userId);
        Task<List<Organization>> GetAllOrganizationsAsync();
        Task<Organization> IsInOrganizationById(Guid userId);
        Task<OrganizationViewModel> GetOrganizationViewModelByUserId(Guid id);
        Task<Organization> GetByIdAsync(int id);

        System.Threading.Tasks.Task UpdateAsync(Organization organization);

        Task<bool> DeleteUserFromOrganizationAsync(Guid userId, int organizationId);
    }
}
