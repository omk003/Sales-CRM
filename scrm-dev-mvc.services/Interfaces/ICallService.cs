namespace scrm_dev_mvc.services.Interfaces
{
    public interface ICallService
    {
        Task<string> MakeCallAsync(string toPhoneNumber, Guid userId, int contactId);
    }

}
