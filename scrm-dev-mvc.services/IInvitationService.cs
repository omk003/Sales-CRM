using scrm_dev_mvc.Models;

namespace scrm_dev_mvc.services
{
    public interface IInvitationService
    {
        Task<Invitation> CreateInvitationAsync(string email, int organizationId, int roleId);
        Task<bool> AcceptInvitationAsync(string invitationCode, Guid userId);
        Task<Invitation?> GetInvitationByCodeAsync(string invitationCode);
    }
}