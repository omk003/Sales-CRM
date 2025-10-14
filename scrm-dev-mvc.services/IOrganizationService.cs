using scrm_dev_mvc.Models;
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
    }
}
