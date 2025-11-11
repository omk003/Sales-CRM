using scrm_dev_mvc.Models;
using System.Linq.Expressions;

namespace scrm_dev_mvc.Data.Repository.IRepository
{
    public interface IRepository<T> where T : class
    {
        Task<T> GetByIdAsync(Guid id);
        Task<IEnumerable<T>> GetAllAsync();
        List<T> GetAll();
        Task<List<T>> GetAllAsync(System.Linq.Expressions.Expression<Func<T, bool>> predicate, bool asNoTracking = false, params Expression<Func<T, object>>[] includes);
        System.Threading.Tasks.Task AddAsync(T entity);
        void Delete(T entity);
        System.Threading.Tasks.Task SaveChangesAsync();
        Task<T> FirstOrDefaultAsync(System.Linq.Expressions.Expression<Func<T, bool>> predicate, string? include = null);

        Task<T> FindAsync(Guid id);

        Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);

        void DeleteRange(List<T> entity);


    }
}
