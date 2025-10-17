using scrm_dev_mvc.Data.Repository.IRepository;
using scrm_dev_mvc.Models;
using scrm_dev_mvc.Models.ViewModels;
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

        public async Task<OrganizationViewModel> GetOrganizationViewModelByUserId(Guid id)
        {
            var user = await unitOfWork.Users.FirstOrDefaultAsync(u => u.Id == id, "Role");

            if (user == null || user.OrganizationId == null)
            {
                return null;
            }
            var organization = await unitOfWork.Organization.FirstOrDefaultAsync(o => o.Id == user.OrganizationId);
            var usersInOrg = (await unitOfWork.Users.GetAllAsync(
                u => u.OrganizationId == user.OrganizationId,
                true,
                u => u.Role
            ))
            .Select(u => new UserInOrganizationViewModel
            {
                UserId = u.Id,
                FullName = ((u.FirstName ?? "") + " " + (u.LastName ?? "")).Trim(),
                Email = u.Email,
                Role = u.Role.Name, 
            })
            .ToList();
            var organizationViewModel = new OrganizationViewModel
            {
                OrganizationId = organization.Id,
                OrganizationName = organization.Name,
                CurrentUserRole = user.Role.Name,
                Users = usersInOrg
            };
            return organizationViewModel;
        }

        public async Task<Organization> GetByIdAsync(int id)
        {
            return await unitOfWork.Organization.FirstOrDefaultAsync(u => u.Id == id);
        }

        public async System.Threading.Tasks.Task UpdateAsync(Organization organization)
        {
            unitOfWork.Organization.Update(organization);
            await unitOfWork.SaveChangesAsync();
        }

        public async Task<bool> DeleteUserFromOrganizationAsync(Guid userId, int organizationId)
        {
            var user = await unitOfWork.Users.FirstOrDefaultAsync(u => u.Id == userId && u.OrganizationId == organizationId);
            //TODO: Check if there are contacts attached to User
            if(user == null)
            {
                return false;
            }
            user.OrganizationId = null;
            await unitOfWork.SaveChangesAsync();
            return true;
        }
    }
}
