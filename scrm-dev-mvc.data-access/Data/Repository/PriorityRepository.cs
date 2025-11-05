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
    public class PriorityRepository : Repository<scrm_dev_mvc.Models.Priority>, IPriorityRepository
    {
        private readonly ApplicationDbContext _db;
        public PriorityRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }
    }
}
