using Microsoft.EntityFrameworkCore.Update.Internal;
using scrm_dev_mvc.Data.Repository.IRepository;
using scrm_dev_mvc.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace scrm_dev_mvc.data_access.Data.Repository.IRepository
{
    public interface IEmailThreadRepository : IRepository<EmailThread>
    {
        public void Update(EmailThread emailThread);
    }
}
