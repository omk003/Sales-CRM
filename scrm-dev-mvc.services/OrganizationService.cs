using scrm_dev_mvc.Data.Repository.IRepository;
using scrm_dev_mvc.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace scrm_dev_mvc.services
{
    public class OrganizationService(IUnitOfWork unitOfWork):IOrganizationService
    {
        public async System.Threading.Tasks.Task CreateOrganizationAsync(Organization organization, Guid userId)
        {
            var currentUser = await unitOfWork.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (currentUser == null)
            {
                return;
            }

            await unitOfWork.Organization.AddAsync(organization);
            await unitOfWork.SaveChangesAsync(); 

            currentUser.OrganizationId = organization.Id;
            currentUser.RoleId = 2; // Super admin role

            await unitOfWork.SaveChangesAsync();
        }

        public async Task<List<Organization>> GetAllOrganizationsAsync()
        {
            return (await unitOfWork.Organization.GetAllAsync()).ToList();
        }

        public async Task<Organization> IsInOrganizationById(Guid userId)
        {
            var user = await unitOfWork.Users.FirstOrDefaultAsync(u => u.Id == userId, "Organization");
            return user.Organization;
        }
    }
}
