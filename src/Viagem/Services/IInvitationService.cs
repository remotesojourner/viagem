using Viagem.Data.Models;

namespace Viagem.Services;

public interface IInvitationService
{
    Task<List<Invitation>> GetPendingInvitationsAsync(string userId);
    Task<List<Invitation>> GetPendingInvitationsByEmailAsync(string email);
    Task<Invitation> CreateInvitationAsync(int tripId, string fromUserId, string toEmail, string? message);
    Task AcceptInvitationAsync(string token, string userId);
    Task DeclineInvitationAsync(string token);
    Task<Invitation?> GetByTokenAsync(string token);
}
