using Microsoft.EntityFrameworkCore.Metadata;
using scrm_dev_mvc.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace scrm_dev_mvc.services
{
    public interface IUserService
    {
        Task<IEnumerable<Models.User>> GetAllUsersAsync();

        Task<List<User>> GetAllUsersByOrganizationIdAsync(int id);
        Task<User> IsEmailExistsAsync(string email);

        System.Threading.Tasks.Task CreateUserAsync(User user);
        Task<User> GetUserByIdAsync(Guid id);
        System.Threading.Tasks.Task UpdateUserAsync(User user);

        Task<bool> AssignUserToOrganizationAsync(Guid userId, int organizationId, int roleId);

        Task<User> GetFirstOrDefault(System.Linq.Expressions.Expression<Func<User, bool>> predicate, string? Include);
        Task<bool> ChangeUserRoleAsync(Guid userId, int organizationId, string newRole);
    }
}
