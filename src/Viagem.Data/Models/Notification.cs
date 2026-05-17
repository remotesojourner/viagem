using System.ComponentModel.DataAnnotations;

namespace Viagem.Data.Models;

public class Notification
{
    public int Id { get; set; }

    public string UserId { get; set; } = "";
    public ApplicationUser? User { get; set; }

    [Required]
    public string Subject { get; set; } = "";

    public string? Message { get; set; }
    public string? Sender { get; set; }
    public bool Read { get; set; } = false;
    public DateTime? ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
