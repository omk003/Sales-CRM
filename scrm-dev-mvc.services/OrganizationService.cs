using Microsoft.Extensions.Logging;
using scrm_dev_mvc.Data.Repository;
using scrm_dev_mvc.Data.Repository.IRepository;
using scrm_dev_mvc.Models;
using scrm_dev_mvc.Models.DTO;
using scrm_dev_mvc.Models.ViewModels;
using scrm_dev_mvc.services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace scrm_dev_mvc.services
{
    public class OrganizationService(IUnitOfWork unitOfWork, ILogger<OrganizationService> _logger, IContactService contactService):IOrganizationService
    {
        public async System.Threading.Tasks.Task CreateOrganizationAsync(Organization organization, Guid userId)
        {
            var currentUser = await unitOfWork.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (currentUser == null)
            {
                _logger.LogWarning("CreateOrganizationAsync failed: User {UserId} not found.", userId);
                return;
            }

            await unitOfWork.Organization.AddAsync(organization);
            await unitOfWork.SaveChangesAsync(); 

            try
            {
                var oldOrgId = currentUser.OrganizationId.ToString();
                var oldRoleId = currentUser.RoleId.ToString();

                currentUser.OrganizationId = organization.Id;
                currentUser.RoleId = 2; // Super admin role
                unitOfWork.Users.Update(currentUser);

                await unitOfWork.SaveChangesAsync(); 
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Organization {OrgId} was created, but failed to update user or create audit logs.", organization.Id);
            }
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

        public async Task<bool> UpdateOrganizationAsync(OrganizationUpdateDto dto, Guid ownerId)
        {
            var organization = await unitOfWork.Organization.FirstOrDefaultAsync(u=>u.Id == dto.OrganizationId);
            if (organization == null)
            {
                _logger.LogWarning("UpdateOrganizationAsync failed: Org {OrgId} not found.", dto.OrganizationId);
                return false;
            }

            try
            {
                if (organization.Name != dto.Name)
                {
                   
                    organization.Name = dto.Name;
                }

                if (organization.Address != dto.Address)
                {
                    
                    organization.Address = dto.Address;
                }

                if (organization.PhoneNumber != dto.PhoneNumber)
                {
                    
                    organization.PhoneNumber = dto.PhoneNumber;
                }

                unitOfWork.Organization.Update(organization);
                await unitOfWork.SaveChangesAsync(); 
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating organization {OrgId}", dto.OrganizationId);
                return false;
            }
        }


        public async Task<bool> DeleteUserFromOrganizationAsync(Guid userId, int organizationId, Guid adminId)
        {
            var user = await unitOfWork.Users.FirstOrDefaultAsync(u => u.Id == userId && u.OrganizationId == organizationId);
            if (user == null)
            {
                _logger.LogWarning("DeleteUserFromOrganizationAsync failed: User {UserId} not found in org {OrgId}", userId, organizationId);
                return false;
            }

            var oldOrgId = user.OrganizationId.ToString(); 

            try
            {
                user.OrganizationId = null;
                unitOfWork.Users.Update(user);

                await unitOfWork.SaveChangesAsync(); 
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId} from org {OrgId}", userId, organizationId);
                return false;
            }
        }

        public async Task<bool> ReassignAndRemoveUserAsync(Guid userIdToRemove, int organizationId, Guid newOwnerId, Guid adminId)
        {
            var userToRemove = await unitOfWork.Users.FirstOrDefaultAsync(u => u.Id == userIdToRemove && u.OrganizationId == organizationId);
            if (userToRemove == null)
            {
                _logger.LogWarning("ReassignAndRemoveUserAsync failed: User {UserId} not found in org {OrgId}", userIdToRemove, organizationId);
                return false;
            }

            var newOwner = await unitOfWork.Users.FirstOrDefaultAsync(u => u.Id == newOwnerId && u.OrganizationId == organizationId);
            if (newOwner == null)
            {
                _logger.LogWarning("ReassignAndRemoveUserAsync failed: New owner {NewOwnerId} not found in org {OrgId}", newOwnerId, organizationId);
                return false;
            }

            if (userIdToRemove == newOwnerId)
            {
                _logger.LogWarning("ReassignAndRemoveUserAsync failed: Cannot re-assign user to themselves.");
                return false;
            }

            try
            {
                var oldOrgId = userToRemove.OrganizationId.ToString(); 

                var contactsToReassign = await contactService.GetAllContactsByOwnerIdAsync(userIdToRemove);
                foreach (var contact in contactsToReassign)
                {
                    contact.OwnerId = newOwnerId;
                    unitOfWork.Contacts.Update(contact);
                }
                _logger.LogInformation("Re-assigning {Count} contacts from user {OldOwner} to {NewOwner}", contactsToReassign.Count, userIdToRemove, newOwnerId);

                var completedStatus = await unitOfWork.TaskStatuses.FirstOrDefaultAsync(s => s.StatusName == "Completed");
                int completedStatusId = completedStatus?.Id ?? -1;
                var tasksToReassign = await unitOfWork.Tasks.GetAllAsync(
                    t => t.OwnerId == userIdToRemove && t.StatusId != completedStatusId
                );
                foreach (var task in tasksToReassign)
                {
                    task.OwnerId = newOwnerId;
                    unitOfWork.Tasks.Update(task);
                }
                _logger.LogInformation("Re-assigning {Count} incomplete tasks from user {OldOwner} to {NewOwner}", tasksToReassign.Count, userIdToRemove, newOwnerId);

                var dealsToReassign = await unitOfWork.Deals.GetAllAsync(d => d.OwnerId == userIdToRemove);
                foreach (var deal in dealsToReassign)
                {
                    deal.OwnerId = newOwnerId;
                    unitOfWork.Deals.Update(deal);
                }
                _logger.LogInformation("Re-assigning {Count} deals from user {OldOwner} to {NewOwner}", dealsToReassign.Count, userIdToRemove, newOwnerId);

                var companiesToReassign = await unitOfWork.Company.GetAllAsync(c => c.UserId == userIdToRemove);
                foreach (var company in companiesToReassign)
                {
                    company.UserId = newOwnerId;
                    unitOfWork.Company.Update(company);
                }
                _logger.LogInformation("Re-assigning {Count} companies from user {OldOwner} to {NewOwner}", companiesToReassign.Count, userIdToRemove, newOwnerId);
                

                userToRemove.OrganizationId = null;
                userToRemove.RoleId = 2; 
                unitOfWork.Users.Update(userToRemove);

                await unitOfWork.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error re-assigning and deleting user {UserId} from org {OrgId}", userIdToRemove, organizationId);
                return false;
            }
        }

       
    }
}
