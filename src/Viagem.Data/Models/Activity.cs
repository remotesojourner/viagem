using System.ComponentModel.DataAnnotations;

namespace Viagem.Data.Models;

public class Activity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(200)]
    public string Name { get; set; } = "";

    public string? Description { get; set; }
    public string? Address { get; set; }
    public string? Notes { get; set; }
    public string? Link { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Timezone { get; set; }

    // Cost properties removed (UI concern bleeding into model)

    public int? PlaceId { get; set; }

    public Guid? ExpenseId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<Guid> AttachmentIds { get; set; } = [];
    public List<int> TravellerProfileIds { get; set; } = [];
}
