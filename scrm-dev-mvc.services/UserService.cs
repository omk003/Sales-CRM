using scrm_dev_mvc.Data.Repository;
using scrm_dev_mvc.Data.Repository.IRepository;
using scrm_dev_mvc.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace scrm_dev_mvc.services
{
    public class UserService(IUnitOfWork unitOfWork): IUserService
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

        public async System.Threading.Tasks.Task CreateUserAsync(User user)
        {
            await unitOfWork.Users.AddAsync(user);
            await unitOfWork.SaveChangesAsync(); 
        }

        public Task<User> GetUserByIdAsync(Guid id)
        {

            return unitOfWork.Users.GetByIdAsync(id);
        }

        public async Task<User> GetFirstOrDefault(System.Linq.Expressions.Expression<Func<User, bool>> predicate, string? Include)
        {
            return await unitOfWork.Users.FirstOrDefaultAsync(predicate, Include);
        }

        

        public async System.Threading.Tasks.Task UpdateUserAsync(User user)
        {
            unitOfWork.Users.Update(user);
            await unitOfWork.SaveChangesAsync();
        }


        public async Task<bool> AssignUserToOrganizationAsync(Guid userId, int organizationId, int roleId)
        {
            var user = await unitOfWork.Users.GetByIdAsync(userId);
            if (user == null)
            {
                // User not found, operation failed
                return false;
            }

            user.OrganizationId = organizationId;
            user.RoleId = roleId;
            await unitOfWork.SaveChangesAsync();

            // Operation was successful
            return true;
        }
    }
}
