using scrm_dev_mvc.Data.Repository.IRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace scrm_dev_mvc.data_access.Data.Repository.IRepository
{
    public interface IDealRepository: IRepository<scrm_dev_mvc.Models.Deal>
    {
        public void Update(scrm_dev_mvc.Models.Deal deal);
    }
}
