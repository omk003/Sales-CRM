using scrm_dev_mvc.Models.DTO;
using scrm_dev_mvc.Models;
using System.Threading.Tasks;

namespace scrm_dev_mvc.Services
{
    public interface IActivityService
    {
        Task<Activity> CreateActivityAsync(CreateActivityDto activityDto);

        Task<IEnumerable<Activity>> GetActivitiesByContactAsync(int contactId);
        Task<IEnumerable<Activity>> GetActivitiesByDealAsync(int dealId);
        Task<IEnumerable<Activity>> GetActivitiesByCompanyAsync(int companyId);
    }
}