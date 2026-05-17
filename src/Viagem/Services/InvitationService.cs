using Viagem.Data.Models;

namespace Viagem.Services;

// Invitation system has been removed. This stub keeps the interface satisfied.
public class InvitationService : IInvitationService
{
    public Task<List<Invitation>> GetPendingInvitationsAsync(string userId) => Task.FromResult(new List<Invitation>());
    public Task<List<Invitation>> GetPendingInvitationsByEmailAsync(string email) => Task.FromResult(new List<Invitation>());
    public Task<Invitation> CreateInvitationAsync(int tripId, string fromUserId, string toEmail, string? message) => throw new NotSupportedException("Invitations have been removed.");
    public Task AcceptInvitationAsync(string token, string userId) => Task.CompletedTask;
    public Task DeclineInvitationAsync(string token) => Task.CompletedTask;
    public Task<Invitation?> GetByTokenAsync(string token) => Task.FromResult<Invitation?>(null);
}
