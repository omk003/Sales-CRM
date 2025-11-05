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

        IActivityTypeRepository ActivityTypes { get; }

        IEmailMessageRepository EmailMessages { get; }

        IEmailThreadRepository EmailThreads { get; }

        ICallRepository Calls { get; }

        IDealRepository Deals { get; }

        IStageRepository Stages { get; }

        ITaskRepository Tasks { get; }

        ITaskStatusRepository TaskStatuses { get; }

        IPriorityRepository Priorities { get; }

        IAuditRepository Audits { get; }
        Task SaveChangesAsync();
    }
}
