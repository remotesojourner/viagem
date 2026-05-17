using System.ComponentModel.DataAnnotations;

namespace Viagem.Data.Models;

public enum InvitationStatus
{
    Pending,
    Accepted,
    Declined,
    Expired
}

public class Invitation
{
    public int Id { get; set; }

    public int TripId { get; set; }
    public Trip? Trip { get; set; }

    public string FromUserId { get; set; } = "";
    public ApplicationUser? FromUser { get; set; }

    [Required, EmailAddress]
    public string ToEmail { get; set; } = "";

    public string? ToUserId { get; set; }
    public ApplicationUser? ToUser { get; set; }

    public string? Message { get; set; }
    public InvitationStatus Status { get; set; } = InvitationStatus.Pending;
    public string Token { get; set; } = Guid.NewGuid().ToString();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; } = DateTime.UtcNow.AddDays(7);
    public DateTime? RespondedAt { get; set; }
}
