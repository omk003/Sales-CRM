using scrm_dev_mvc.Models;
using scrm_dev_mvc.Models.ViewModels;

namespace scrm_dev_mvc.Services
{
    public interface IContactService
    {
        Task<string> CreateContactAsync(ContactDto contactDto);

        Task<List<ContactResponseViewModel>> GetAllContacts(Guid userId);

        Task<IEnumerable<LeadStatus>> GetLeadStatusesAsync();
        Task<IEnumerable<Lifecycle>> GetLifeCycleStagesAsync();

        Task<bool> DeleteContactsByIdsAsync(List<int> ids);
        Task<string> UpdateContact(ContactDto contact);

        Contact GetContactById(int id);

        Task<bool> AssociateContactToCompany(int contactId, int companyId);

        Task<bool> AssociateContactToDealAsync(int contactId, int dealId);

        //Task<(List<ContactResponseViewModel> Result, long WithTrackingMs, long WithoutTrackingMs)> GetAllContactsPerformanceTest(Guid userId);
    }
}
