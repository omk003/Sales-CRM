using scrm_dev_mvc.data_access.Data.Repository.IRepository;
using scrm_dev_mvc.DataAccess.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace scrm_dev_mvc.data_access.Data.Repository
{
    public class ActivityRepository: IActivityRepository
    {
        private readonly ApplicationDbContext _context;

        public ActivityRepository(ApplicationDbContext context)
        {
            _context = context;
        }


    }
}
