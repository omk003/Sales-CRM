namespace scrm_dev_mvc.Services
{
    public interface ICallService
    {
        Task<string> MakeCallAsync(string toPhoneNumber, Guid userId, int contactId);
    }

}
