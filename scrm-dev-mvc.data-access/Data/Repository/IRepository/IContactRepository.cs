using scrm_dev_mvc.Models;
namespace scrm_dev_mvc.Data.Repository.IRepository
{
    public interface IContactRepository: IRepository<Contact>
    {
        void Update(Contact contact);
    }
}
