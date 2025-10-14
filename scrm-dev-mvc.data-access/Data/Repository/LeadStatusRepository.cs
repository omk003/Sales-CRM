using scrm_dev_mvc.data_access.Data.Repository.IRepository;
using scrm_dev_mvc.DataAccess.Data;
using scrm_dev_mvc.Models;

namespace scrm_dev_mvc.Data.Repository
{
    public class LeadStatusRepository : Repository<LeadStatus>, ILeadStatusRepository
    {
        private readonly ApplicationDbContext _context;
        public LeadStatusRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }
    }
}