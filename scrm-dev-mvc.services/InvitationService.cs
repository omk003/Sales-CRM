
using Microsoft.EntityFrameworkCore.Metadata;
using scrm_dev_mvc.Data.Repository.IRepository;
using scrm_dev_mvc.DataAccess.Data;
using scrm_dev_mvc.Models;
using scrm_dev_mvc.services.Interfaces;

public class InvitationService(ApplicationDbContext context, IUserService userService,IUnitOfWork unitOfWork) : IInvitationService
{
    public async Task<Invitation> CreateInvitationAsync(string email, int organizationId, int roleId, Guid senderId)
    {
        var existingInvitation = await unitOfWork.Invitations.FirstOrDefaultAsync(inv =>
        inv.Email == email &&
        inv.OrganizationId == organizationId &&
        inv.IsAccepted == false &&
        inv.ExpiryDate > DateTime.UtcNow);

        
        if (existingInvitation != null)
        {
           
            return null;
        }

        var invitation = new Invitation
        {
            Id = Guid.NewGuid(),
            Email = email,
            SenderId = senderId,
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
            return false; 
        }
        var ownerId = userId;  
        var success = await userService.AssignUserToOrganizationAsync(userId, invitation.OrganizationId, invitation.RoleId, ownerId);

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
        return Guid.NewGuid().ToString().Replace("-", "").Substring(0, 8).ToUpper();
    }
}