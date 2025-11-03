using scrm_dev_mvc.Data.Repository;
using scrm_dev_mvc.data_access.Data.Repository.IRepository;
using scrm_dev_mvc.DataAccess.Data;
using scrm_dev_mvc.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace scrm_dev_mvc.data_access.Data.Repository
{
    public class EmailThreadRepository : Repository<EmailThread>, IEmailThreadRepository
    {
        private readonly ApplicationDbContext _context;
        public EmailThreadRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public void Update(EmailThread emailThread)
        {
            _context.EmailThreads.Update(emailThread);
        }
    }
}
