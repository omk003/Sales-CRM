using scrm_dev_mvc.data_access.Data.Repository.IRepository;

namespace scrm_dev_mvc.Data.Repository.IRepository
{
    public interface IUnitOfWork
    {
        IUserRepository Users { get; }
        IContactRepository Contacts { get;  }

        IGmailCred GmailCred { get; }

        ILeadStatusRepository LeadStatuses { get; }

        ILifecycleRepository Lifecycle { get; }

        IOrganizationRepository Organization { get; }

        ICompanyRepository Company { get; }

        IInvitationRepository Invitations { get; }

        IActivityRepository Activities { get; }
        Task SaveChangesAsync();
    }
}
