using scrm_dev_mvc.Data.Repository.IRepository;
using scrm_dev_mvc.data_access.Data.Repository;
using scrm_dev_mvc.data_access.Data.Repository.IRepository;
using scrm_dev_mvc.DataAccess.Data;

namespace scrm_dev_mvc.Data.Repository
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        public IUserRepository Users { get; private set; }
        public IContactRepository Contacts { get; private set; }

        public IGmailCred GmailCred { get; private set; }
        public ILeadStatusRepository LeadStatuses { get; private set; }

        public ILifecycleRepository Lifecycle { get; private set; }
        public IOrganizationRepository Organization { get; private set; }

        public ICompanyRepository Company { get; private set; }

        public IInvitationRepository Invitations { get; private set; }

        public IActivityRepository Activities { get; private set; }
        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;
            Users = new UserRepository(_context);
            Contacts = new ContactRepository(_context);
            GmailCred = new GmailCredRepository(_context);
            LeadStatuses = new LeadStatusRepository(_context);
            Lifecycle = new LifecycleRepository(_context);
            Organization = new OrganizationRepository(_context);
            Company = new CompanyRepository(_context);
            Invitations = new InvitationRepository(_context);
            Activities = new ActivityRepository(_context);
        }
        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
