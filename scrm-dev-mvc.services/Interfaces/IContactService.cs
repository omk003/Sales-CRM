using scrm_dev_mvc.Models;
using scrm_dev_mvc.Models.ViewModels;

namespace scrm_dev_mvc.services.Interfaces
{
    public interface IContactService
    {
        Task<string> CreateContactAsync(ContactDto contactDto);

        Task<List<ContactResponseViewModel>> GetAllContacts(Guid userId);

        Task<IEnumerable<LeadStatus>> GetLeadStatusesAsync();
        Task<IEnumerable<Lifecycle>> GetLifeCycleStagesAsync();

        Task<bool> DeleteContactsByIdsAsync(List<int> ids, Guid ownerId);
        Task<string> UpdateContact(ContactDto contact, Guid ownerId);

        Contact GetContactById(int id);

        Task<(bool Success, string Message)> AssociateContactToCompany(int contactId, int companyId, Guid ownerId);

        Task<(bool Success, string Message)> AssociateContactToDealAsync(int contactId, int dealId, Guid ownerId);
        Task<(bool Success, string Message)> DisassociateCompany(int contactId, Guid ownerId);

        Task<List<Contact>> GetAllContactsByOwnerIdAsync(Guid ownerId);

        Task<List<ContactResponseViewModel>> GetAllContactsForCompany(Guid userId, int? companyId);
        Task<(bool Success, string Message)> DisassociateContactFromDealAsync(int contactId, int dealId, Guid ownerId);

        //Task<(List<ContactResponseViewModel> Result, long WithTrackingMs, long WithoutTrackingMs)> GetAllContactsPerformanceTest(Guid userId);
    }
}
