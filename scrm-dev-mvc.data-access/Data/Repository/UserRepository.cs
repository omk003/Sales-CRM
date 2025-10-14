using Microsoft.EntityFrameworkCore;
using scrm_dev_mvc.Data.Repository.IRepository;
using scrm_dev_mvc.DataAccess.Data;
using scrm_dev_mvc  .Models;

namespace scrm_dev_mvc.Data.Repository
{
    public class UserRepository: Repository<User>, IUserRepository
    {
        private readonly ApplicationDbContext _context;
        public UserRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<bool> AnyAsync(System.Linq.Expressions.Expression<Func<User, bool>> predicate)
        {
            return await _context.Users.AnyAsync(predicate);
        }

        public void Update(User user)
        {
            _context.Users.Update(user);
        }
    }
}
