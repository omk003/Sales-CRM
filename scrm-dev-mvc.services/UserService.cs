using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using scrm_dev_mvc.Data.Repository;
using scrm_dev_mvc.Data.Repository.IRepository;
using scrm_dev_mvc.Models;
using scrm_dev_mvc.Models.DTO;
using scrm_dev_mvc.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace scrm_dev_mvc.services
{
    public class UserService(IUnitOfWork unitOfWork, ILogger<UserService> _logger, IAuditService _auditService): IUserService
    {
        // Implement user-related methods here
      
        Task<IEnumerable<User>> IUserService.GetAllUsersAsync()
        {
            return unitOfWork.Users.GetAllAsync();
        }
        Task<List<User>> IUserService.GetAllUsersByOrganizationIdAsync(int id)
        {
            return unitOfWork.Users.GetAllAsync(u => u.OrganizationId == id);
        }
        Task<User> IUserService.IsEmailExistsAsync(string email)
        {
            var user = unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == email,"Role");
            return user;
        }

        public Task<User> GetUserByIdAsync(Guid id)
        {

            return unitOfWork.Users.GetByIdAsync(id);
        }

        public async Task<User> GetFirstOrDefault(System.Linq.Expressions.Expression<Func<User, bool>> predicate, string? Include)
        {
            return await unitOfWork.Users.FirstOrDefaultAsync(predicate, Include);
        }


        // --- CreateUserAsync now logs the creation ---
        public async System.Threading.Tasks.Task CreateUserAsync(User user)
        {
            await unitOfWork.Users.AddAsync(user);
            await unitOfWork.SaveChangesAsync(); // Save to get the user.Id

            // We can't log this against an OrgId yet, as it's not assigned.
            // We'll log against a "placeholder" RecordId 0.
            try
            {
                await _auditService.LogChangeAsync(new AuditLogDto
                {
                    OwnerId = user.Id, // User "created" themselves
                    RecordId = 0, // No orgId to log against yet
                    TableName = "User",
                    FieldName = "User",
                    OldValue = "[NULL]",
                    NewValue = $"Created new user: {user.Email} (Id: {user.Id})"
                });
                await unitOfWork.SaveChangesAsync(); // Save the audit log
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "User {UserId} was created, but auditing failed.", user.Id);
            }
        }

        public async System.Threading.Tasks.Task UpdateUserAsync(User user)
        {
            unitOfWork.Users.Update(user);
            await unitOfWork.SaveChangesAsync();
        }

        public async Task<bool> UpdateUserProfileAsync(Guid userId, string newFirstName, string newLastName,Guid ownerId)
        {
            var user = await unitOfWork.Users.GetByIdAsync(userId);
            if (user == null || !user.OrganizationId.HasValue) return false;

            int orgId = user.OrganizationId.Value; // Get the OrgId to log against

            try
            {
                if (user.FirstName != newFirstName)
                {
                    await LogChange(ownerId, orgId, userId, "FirstName", user.FirstName, newFirstName);
                    user.FirstName = newFirstName;
                }
                if (user.LastName != newLastName)
                {
                    await LogChange(ownerId, orgId, userId, "LastName", user.LastName, newLastName);
                    user.LastName = newLastName;
                }

                unitOfWork.Users.Update(user);
                await unitOfWork.SaveChangesAsync(); // Saves User changes and Audit logs
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile for user {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> AssignUserToOrganizationAsync(Guid userId, int organizationId, int roleId, Guid adminId)
        {
            var user = await unitOfWork.Users.GetByIdAsync(userId);
            if (user == null) return false;

            try
            {
                // Log Organization change
                await LogChange(adminId, organizationId, userId, "OrganizationId", user.OrganizationId.ToString(), organizationId.ToString());

                // Log Role change
                await LogChange(adminId, organizationId, userId, "RoleId", user.RoleId.ToString(), roleId.ToString());

                user.OrganizationId = organizationId;
                user.RoleId = roleId;

                await unitOfWork.SaveChangesAsync(); // Saves User changes and Audit logs
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning user {UserId} to org {OrgId}", userId, organizationId);
                return false;
            }
        }

        // --- UPDATED to include adminId for auditing ---
        public async Task<bool> ChangeUserRoleAsync(Guid userId, int organizationId, string newRole, Guid adminId)
        {
            var user = await unitOfWork.Users
                .FirstOrDefaultAsync(u => u.Id == userId && u.OrganizationId == organizationId, "Role");

            if (user == null)
                return false;

            var oldRoleName = user.Role?.Name ?? "N/A";
            var oldRoleId = user.RoleId.ToString();
            int newRoleId;

            // Map role string to RoleId
            if (newRole == "SalesAdmin")
                newRoleId = 3; // <-- Use your actual admin RoleId
            else if (newRole == "SalesUser")
                newRoleId = 4; // <-- Use your actual user RoleId
            else
                return false;

            try
            {
                // Log the role change
                await LogChange(adminId, organizationId, userId, "RoleId", oldRoleId, newRoleId.ToString());

                user.RoleId = newRoleId;
                await unitOfWork.SaveChangesAsync(); // Saves User change and Audit log
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing role for user {UserId}", userId);
                return false;
            }
        }

        // --- Private Helper Method for Auditing ---
        private async System.Threading.Tasks.Task LogChange(Guid ownerId, int organizationId, Guid userId, string field, string oldVal, string newVal)
        {
            await _auditService.LogChangeAsync(new AuditLogDto
            {
                OwnerId = ownerId,
                RecordId = organizationId, // Log against the Organization
                TableName = "User",
                FieldName = $"{field} (User: {userId})", // Store the Guid here
                OldValue = oldVal,
                NewValue = newVal
            });
        }
    }
}
