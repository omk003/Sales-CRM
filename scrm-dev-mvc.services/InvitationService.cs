// You'll need to inject your DbContext and IUserService here
// For this example, let's assume you have a 'ApplicationDbContext' and 'IUserService'
using Microsoft.EntityFrameworkCore.Metadata;
using scrm_dev_mvc.Data.Repository.IRepository;
using scrm_dev_mvc.DataAccess.Data;
using scrm_dev_mvc.Models;
using scrm_dev_mvc.services;

public class InvitationService(ApplicationDbContext context, IUserService userService,IUnitOfWork unitOfWork) : IInvitationService
{
    public async Task<Invitation> CreateInvitationAsync(string email, int organizationId, int roleId)
    {
        var invitation = new Invitation
        {
            Id = Guid.NewGuid(),
            Email = email,
            OrganizationId = organizationId,
            RoleId = roleId,
            InvitationCode = GenerateUniqueCode(), 
            ExpiryDate = DateTime.UtcNow.AddDays(7), 
            IsAccepted = false
        };

        await unitOfWork.Invitations.AddAsync(invitation);
        await unitOfWork.SaveChangesAsync();

        return invitation;
    }

    public async Task<bool> AcceptInvitationAsync(string invitationCode, Guid userId)
    {
        var invitation = await unitOfWork.Invitations
            .FirstOrDefaultAsync(i => i.InvitationCode == invitationCode && !i.IsAccepted && i.ExpiryDate > DateTime.UtcNow);

        if (invitation == null)
        {
            return false; // Invalid, used, or expired code
        }

        // Use a user service to update the user's organization
        var success = await userService.AssignUserToOrganizationAsync(userId, invitation.OrganizationId, invitation.RoleId);

        if (success)
        {
            invitation.IsAccepted = true;
            await context.SaveChangesAsync();
            return true;
        }

        return false;
    }

    public async Task<Invitation?> GetInvitationByCodeAsync(string invitationCode)
    {
        return await unitOfWork.Invitations.FirstOrDefaultAsync(i => i.InvitationCode == invitationCode);
    }

    private string GenerateUniqueCode()
    {
        // Generates a random 8-character alphanumeric code
        return Guid.NewGuid().ToString().Replace("-", "").Substring(0, 8).ToUpper();
    }
}