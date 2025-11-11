using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace scrm_dev_mvc.services.Interfaces
{
    public interface IDealService
    {
        Task<bool> UpdateDealStageAsync(int dealId, string newStageName);
    }
}
