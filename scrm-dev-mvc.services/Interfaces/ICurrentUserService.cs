using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace scrm_dev_mvc.services.Interfaces
{
    public interface ICurrentUserService
    {
        Guid GetUserId();
        // You could also add other properties like:
        string GetUserEmail();
        bool IsAuthenticated();

        bool IsInRole(string role);
    }
}
