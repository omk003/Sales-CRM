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
    public class OrganizationService(IUnitOfWork unitOfWork, ILogger<OrganizationService> _logger, IAuditService _auditService, IContactService contactService):IOrganizationService
    {
        public async System.Threading.Tasks.Task CreateOrganizationAsync(Organization organization, Guid userId)
        {
            var currentUser = await unitOfWork.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (currentUser == null)
            {
                _logger.LogWarning("CreateOrganizationAsync failed: User {UserId} not found.", userId);
                return;
            }

            // 1. Save the new organization to get its ID
            await unitOfWork.Organization.AddAsync(organization);
            await unitOfWork.SaveChangesAsync(); // First save

            // 2. Now update the user and create all audit logs in a second transaction
            try
            {
                var oldOrgId = currentUser.OrganizationId.ToString();
                var oldRoleId = currentUser.RoleId.ToString();

                currentUser.OrganizationId = organization.Id;
                currentUser.RoleId = 2; // Super admin role
                unitOfWork.Users.Update(currentUser);

                // --- AUDIT LOGIC ---
                // Log Organization creation
                await _auditService.LogChangeAsync(new AuditLogDto
                {
                    OwnerId = userId,
                    RecordId = organization.Id,
                    TableName = "Organization",
                    FieldName = "Organization",
                    OldValue = "[NULL]",
                    NewValue = $"Created new organization: {organization.Name}"
                });
                // Log User's OrgId change
                await _auditService.LogChangeAsync(new AuditLogDto
                {
                    OwnerId = userId,
                    RecordId = organization.Id,
                    TableName = "User",
                    FieldName = $"OrganizationId (User: {userId})",
                    OldValue = oldOrgId,
                    NewValue = organization.Id.ToString()
                });
                // Log User's RoleId change
                await _auditService.LogChangeAsync(new AuditLogDto
                {
                    OwnerId = userId,
                    RecordId = organization.Id,
                    TableName = "User",
                    FieldName = $"RoleId (User: {userId})",
                    OldValue = oldRoleId,
                    NewValue = "2" // Super Admin
                });
                // --- END AUDIT LOGIC ---

                await unitOfWork.SaveChangesAsync(); // Second save (User update + 3 Audit logs)
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Organization {OrgId} was created, but failed to update user or create audit logs.", organization.Id);
                // Optionally, you could try to roll back the organization creation here.
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
                    await _auditService.LogChangeAsync(new AuditLogDto
                    {
                        OwnerId = ownerId,
                        RecordId = organization.Id,
                        TableName = "Organization",
                        FieldName = "Name",
                        OldValue = organization.Name,
                        NewValue = dto.Name
                    });
                    organization.Name = dto.Name;
                }

                if (organization.Address != dto.Address)
                {
                    await _auditService.LogChangeAsync(new AuditLogDto
                    {
                        OwnerId = ownerId,
                        RecordId = organization.Id,
                        TableName = "Organization",
                        FieldName = "Address",
                        OldValue = organization.Address,
                        NewValue = dto.Address
                    });
                    organization.Address = dto.Address;
                }

                if (organization.PhoneNumber != dto.PhoneNumber)
                {
                    await _auditService.LogChangeAsync(new AuditLogDto
                    {
                        OwnerId = ownerId,
                        RecordId = organization.Id,
                        TableName = "Organization",
                        FieldName = "PhoneNumber",
                        OldValue = organization.PhoneNumber,
                        NewValue = dto.PhoneNumber
                    });
                    organization.PhoneNumber = dto.PhoneNumber;
                }

                unitOfWork.Organization.Update(organization);
                await unitOfWork.SaveChangesAsync(); // Saves Org update + All Audit logs
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

            var oldOrgId = user.OrganizationId.ToString(); // Store for logging

            try
            {
                // --- AUDIT LOGIC ---
                await _auditService.LogChangeAsync(new AuditLogDto
                {
                    OwnerId = adminId,
                    RecordId = organizationId, // Log against the org
                    TableName = "User",
                    FieldName = $"OrganizationId (User: {userId})", // Log which user was changed
                    OldValue = oldOrgId,
                    NewValue = "[NULL]"
                });
                // --- END AUDIT LOGIC ---

                // Apply the change
                user.OrganizationId = null;
                unitOfWork.Users.Update(user);

                await unitOfWork.SaveChangesAsync(); // Save User update + Audit log
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
            // 1. Get the user to be removed
            var userToRemove = await unitOfWork.Users.FirstOrDefaultAsync(u => u.Id == userIdToRemove && u.OrganizationId == organizationId);
            if (userToRemove == null)
            {
                _logger.LogWarning("ReassignAndRemoveUserAsync failed: User {UserId} not found in org {OrgId}", userIdToRemove, organizationId);
                return false;
            }

            // 2. Get the user to re-assign tasks to
            var newOwner = await unitOfWork.Users.FirstOrDefaultAsync(u => u.Id == newOwnerId && u.OrganizationId == organizationId);
            if (newOwner == null)
            {
                _logger.LogWarning("ReassignAndRemoveUserAsync failed: New owner {NewOwnerId} not found in org {OrgId}", newOwnerId, organizationId);
                return false;
            }

            // 3. Check that admin is not re-assigning to the user being deleted
            if (userIdToRemove == newOwnerId)
            {
                _logger.LogWarning("ReassignAndRemoveUserAsync failed: Cannot re-assign user to themselves.");
                return false;
            }

            try
            {
                var oldOrgId = userToRemove.OrganizationId.ToString(); // Store for logging

                // 4. Re-assign Contacts (including soft-deleted)
                var contactsToReassign = await contactService.GetAllContactsByOwnerIdAsync(userIdToRemove);
                foreach (var contact in contactsToReassign)
                {
                    await LogReassignment(adminId, organizationId, "Contact", contact.Id, userIdToRemove, newOwnerId);
                    contact.OwnerId = newOwnerId;
                    unitOfWork.Contacts.Update(contact);
                }
                _logger.LogInformation("Re-assigning {Count} contacts from user {OldOwner} to {NewOwner}", contactsToReassign.Count, userIdToRemove, newOwnerId);

                // 5. Re-assign Incomplete Tasks
                var completedStatus = await unitOfWork.TaskStatuses.FirstOrDefaultAsync(s => s.StatusName == "Completed");
                int completedStatusId = completedStatus?.Id ?? -1;
                var tasksToReassign = await unitOfWork.Tasks.GetAllAsync(
                    t => t.OwnerId == userIdToRemove && t.StatusId != completedStatusId
                );
                foreach (var task in tasksToReassign)
                {
                    await LogReassignment(adminId, organizationId, "Task", task.Id, userIdToRemove, newOwnerId);
                    task.OwnerId = newOwnerId;
                    unitOfWork.Tasks.Update(task);
                }
                _logger.LogInformation("Re-assigning {Count} incomplete tasks from user {OldOwner} to {NewOwner}", tasksToReassign.Count, userIdToRemove, newOwnerId);

                // 6. Re-assign Deals
                var dealsToReassign = await unitOfWork.Deals.GetAllAsync(d => d.OwnerId == userIdToRemove);
                foreach (var deal in dealsToReassign)
                {
                    await LogReassignment(adminId, organizationId, "Deal", deal.Id, userIdToRemove, newOwnerId);
                    deal.OwnerId = newOwnerId;
                    unitOfWork.Deals.Update(deal);
                }
                _logger.LogInformation("Re-assigning {Count} deals from user {OldOwner} to {NewOwner}", dealsToReassign.Count, userIdToRemove, newOwnerId);

                // 7. Re-assign Companies
                var companiesToReassign = await unitOfWork.Company.GetAllAsync(c => c.UserId == userIdToRemove);
                foreach (var company in companiesToReassign)
                {
                    await LogReassignment(adminId, organizationId, "Company", company.Id, userIdToRemove, newOwnerId);
                    company.UserId = newOwnerId;
                    unitOfWork.Company.Update(company);
                }
                _logger.LogInformation("Re-assigning {Count} companies from user {OldOwner} to {NewOwner}", companiesToReassign.Count, userIdToRemove, newOwnerId);
                // 8. Log the removal of the user from the org
                await _auditService.LogChangeAsync(new AuditLogDto
                {
                    OwnerId = adminId,
                    RecordId = organizationId,
                    TableName = "User",
                    FieldName = $"OrganizationId (User: {userIdToRemove})",
                    OldValue = oldOrgId,
                    NewValue = "[NULL]"
                });

                // 8. Apply the change to the user
                userToRemove.OrganizationId = null;
                userToRemove.RoleId = 2; // Also remove their role
                unitOfWork.Users.Update(userToRemove);

                // 9. Save EVERYTHING in one transaction
                await unitOfWork.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error re-assigning and deleting user {UserId} from org {OrgId}", userIdToRemove, organizationId);
                return false;
            }
        }

        private async System.Threading.Tasks.Task LogReassignment(Guid adminId, int orgId, string tableName, int recordId, Guid oldOwnerId, Guid newOwnerId)
        {
            await _auditService.LogChangeAsync(new AuditLogDto
            {
                OwnerId = adminId,
                RecordId = orgId, // Log against the org
                TableName = tableName,
                FieldName = $"OwnerId (Record: {recordId})",
                OldValue = oldOwnerId.ToString(),
                NewValue = newOwnerId.ToString()
            });
        }
    }
}
