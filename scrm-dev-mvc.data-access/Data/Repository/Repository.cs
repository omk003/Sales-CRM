using Microsoft.EntityFrameworkCore;
using scrm_dev_mvc.Data.Repository.IRepository;
using scrm_dev_mvc.DataAccess.Data;
using scrm_dev_mvc.Models;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace scrm_dev_mvc.Data.Repository
{
    public class Repository<T> : IRepository<T> where T : class
    {
        private readonly ApplicationDbContext _context;
        private readonly DbSet<T> _dbSet;

        public Repository(ApplicationDbContext context)
        {
            _context = context;
            _dbSet = _context.Set<T>();
        }
        public async System.Threading.Tasks.Task AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
            
        }

        public void Delete(T entity)
        {
            _dbSet.Remove(entity);
        }

        public List<T> GetAll()
        {
            return _dbSet.ToList();
        }
        public async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();

        }

        //public async Task<List<T>> GetAllAsync(Expression<Func<T, bool>> predicate)
        //{
        //    return await _dbSet.Where(predicate).ToListAsync();
        //}
        public async Task<List<T>> GetAllAsync(
         Expression<Func<T, bool>> predicate,
         bool asNoTracking = false,
         params Expression<Func<T, object>>[] includes)
        {
            IQueryable<T> query = _dbSet;

            if (includes != null)
            {
                foreach (var include in includes)
                {
                    query = query.Include(include);
                }
            }

            query = query.Where(predicate);

            if (asNoTracking)
                query = query.AsNoTracking();

            return await query.ToListAsync();
        }

        public async Task<T> GetByIdAsync(Guid id)
        {
            
            return await _dbSet.FindAsync(id);
            
        }

        public Task<T> FirstOrDefaultAsync(
             Expression<Func<T, bool>> predicate,
             string? include = null)
        {
            IQueryable<T> query = _dbSet;

            if (!string.IsNullOrEmpty(include))
            {
                var includes = include.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var inc in includes)
                {
                    query = query.Include(inc);
                }
            }

            return query.FirstOrDefaultAsync(predicate);
        }


        public async Task<T> FindAsync(Guid id)
        {
            return await _dbSet.FindAsync(id);
        }
        public async System.Threading.Tasks.Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();

        }
        public Task<bool> AnyAsync(Expression<Func<T, bool>> predicate)
        {
            return _dbSet.AnyAsync(predicate);
        }
        public void DeleteRange(List<T> entity)
        {
            _dbSet.RemoveRange(entity);
        }

        public IQueryable<T> GetQueryable()
        {
            
            return _dbSet.AsQueryable();
        }
    }
}
