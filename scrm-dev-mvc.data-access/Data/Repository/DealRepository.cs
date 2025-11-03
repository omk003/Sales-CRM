using scrm_dev_mvc.Data.Repository;
using scrm_dev_mvc.data_access.Data.Repository.IRepository;
using scrm_dev_mvc.DataAccess.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace scrm_dev_mvc.data_access.Data.Repository
{
    public class DealRepository: Repository<scrm_dev_mvc.Models.Deal>, IDealRepository
    {
        private readonly ApplicationDbContext _context;
        public DealRepository(ApplicationDbContext context): base(context)
        {
            _context = context;
        }
        public void Update(scrm_dev_mvc.Models.Deal deal)
        {
            _context.Deals.Update(deal);
        }
    }
}
