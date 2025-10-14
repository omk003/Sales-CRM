using scrm_dev_mvc.Models;

namespace scrm_dev_mvc.Data.Repository.IRepository
{
    public interface IUserRepository: IRepository<Models.User>
    {
        Task<bool> AnyAsync(System.Linq.Expressions.Expression<Func<Models.User, bool>> predicate);
        void Update(User user);
    }
}
