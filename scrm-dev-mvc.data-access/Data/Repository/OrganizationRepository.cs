using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using scrm_dev_mvc.Data.Repository;
using scrm_dev_mvc.Data.Repository.IRepository;
using scrm_dev_mvc.DataAccess.Data;
using scrm_dev_mvc.Models;
namespace scrm_dev_mvc.data_access.Data.Repository
{
    public class OrganizationRepository: Repository<Organization>, IOrganizationRepository
    {
        private readonly ApplicationDbContext _db;
        public OrganizationRepository(ApplicationDbContext db):base(db)
        {
            _db = db;
        }
    }
}
