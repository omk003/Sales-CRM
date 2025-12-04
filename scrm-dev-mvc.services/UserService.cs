using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using scrm_dev_mvc.Data.Repository;
using scrm_dev_mvc.Data.Repository.IRepository;
using scrm_dev_mvc.Models;
using scrm_dev_mvc.Models.DTO;
using scrm_dev_mvc.services.Interfaces;
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


        public async System.Threading.Tasks.Task CreateUserAsync(User user)
        {
            await unitOfWork.Users.AddAsync(user);
            await unitOfWork.SaveChangesAsync(); 

        }

        public async System.Threading.Tasks.Task UpdateUserAsync(User user)
        {
            unitOfWork.Users.Update(user);
            await unitOfWork.SaveChangesAsync();
        }

        public async Task<bool> UpdateUserProfileAsync(Guid userId, string newFirstName, string newLastName,Guid ownerId)
        {
            var user = await unitOfWork.Users.GetByIdAsync(userId);
            if (user == null) return false;
            int orgId;
            if(user.OrganizationId == null)
            {
                orgId = 0;
            }
            else
            {
                orgId = user.OrganizationId.Value;
            }
               
            try
            {
                if (user.FirstName != newFirstName)
                {
                    user.FirstName = newFirstName;
                }
                if (user.LastName != newLastName)
                {
                    user.LastName = newLastName;
                }

                unitOfWork.Users.Update(user);
                await unitOfWork.SaveChangesAsync();
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
                
                user.OrganizationId = organizationId;
                user.RoleId = roleId;

                await unitOfWork.SaveChangesAsync(); 
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning user {UserId} to org {OrgId}", userId, organizationId);
                return false;
            }
        }

        public async Task<bool> ChangeUserRoleAsync(Guid userId, int organizationId, string newRole, Guid adminId)
        {
            var user = await unitOfWork.Users
                .FirstOrDefaultAsync(u => u.Id == userId && u.OrganizationId == organizationId, "Role");

            if (user == null)
                return false;

            var oldRoleName = user.Role?.Name ?? "N/A";
            var oldRoleId = user.RoleId.ToString();
            int newRoleId;

            if (newRole == "SalesAdmin")
                newRoleId = 3; 
            else if (newRole == "SalesUser")
                newRoleId = 4; 
            else
                return false;

            try
            {
                
                user.RoleId = newRoleId;
                await unitOfWork.SaveChangesAsync(); 
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing role for user {UserId}", userId);
                return false;
            }
        }

       

        public async Task<bool> DeleteAsync(User user)
        {
            try
            {
                if (!user.OrganizationId.HasValue)
                {
                    _logger.LogError("Cannot delete user {UserId}: User has no OrganizationId.", user.Id);
                    return false;
                }

                var superAdmin = await unitOfWork.Users.FirstOrDefaultAsync(
                    u => u.OrganizationId == user.OrganizationId &&
                         u.RoleId == 2 
                );

                if (superAdmin == null)
                {
                    _logger.LogError("Cannot delete user {UserId}: No 'SalesAdminSuper' found in organization {OrgId} to transfer ownership to.", user.Id, user.OrganizationId);
                    return false;
                }

                if (superAdmin.Id == user.Id)
                {
                    _logger.LogWarning("Delete attempt failed: User {UserId} is the last/only Super Admin in organization {OrgId}. Cannot delete.", user.Id, user.OrganizationId);
                    return false;
                }

                var gmailCred = await unitOfWork.GmailCred.FirstOrDefaultAsync(gc => gc.Email == user.Email);

                if (gmailCred != null) 
                {
                    unitOfWork.GmailCred.Delete(gmailCred);
                }

                var companies = await unitOfWork.Company.GetAllAsync(
                     c => c.UserId == user.Id
                );

                if (companies.Any())
                {
                    foreach (var company in companies)
                    {
                        company.UserId = superAdmin.Id; 
                        unitOfWork.Company.Update(company); 
                    }
                }

                var contacts = await unitOfWork.Contacts.GetAllAsync(
                    c => c.OwnerId == user.Id
                );

                if (contacts.Any())
                {
                    foreach (var contact in contacts)
                    {
                        contact.OwnerId = superAdmin.Id; 
                        unitOfWork.Contacts.Update(contact);
                    }
                }

                unitOfWork.Users.Delete(user);

                await unitOfWork.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reassigning data and deleting user {UserId}", user.Id);
                return false;
            }
        }
    }

    
}
