using scrm_dev_mvc.Data.Repository.IRepository;
using scrm_dev_mvc.DataAccess.Data;
using scrm_dev_mvc.Models;

namespace scrm_dev_mvc.Data.Repository
{
    public class ContactRepository : Repository<Contact>, IContactRepository
    {
        private readonly ApplicationDbContext _context;
        public ContactRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public void Update(Contact contact)
        {
            _context.Contacts.Update(contact);
        }
    }
}
