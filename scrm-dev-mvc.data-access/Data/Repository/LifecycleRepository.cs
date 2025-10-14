using scrm_dev_mvc.data_access.Data.Repository.IRepository;
using scrm_dev_mvc.DataAccess.Data;
using scrm_dev_mvc.Models;

namespace scrm_dev_mvc.Data.Repository
{
    public class LifecycleRepository : Repository<Lifecycle>, ILifecycleRepository
    {
        private readonly ApplicationDbContext _context;
        public LifecycleRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }
    }
}