using scrm_dev_mvc.Data.Repository;
using scrm_dev_mvc.DataAccess.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace scrm_dev_mvc.data_access.Data.Repository
{
    public class InvitationRepository : Repository<scrm_dev_mvc.Models.Invitation>, IRepository.IInvitationRepository
    {
        public InvitationRepository(ApplicationDbContext context) : base(context)
        {
        }
    }
}
