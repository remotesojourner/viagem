using System.ComponentModel.DataAnnotations;

namespace Viagem.Data.Models;

public enum ExpenseCategory
{
    Transport,
    Accommodation,
    Food,
    Activities,
    Shopping,
    Health,
    Communication,
    Other
}

public class Expense
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(200)]
    public string Name { get; set; } = "";

    public ExpenseCategory? Category { get; set; }
    public string? Notes { get; set; }

    public decimal? Amount { get; set; }
    public string? Currency { get; set; }

    public DateTime? OccurredOn { get; set; }

    /// <summary>"Transportation", "Lodging", "Activity", or null for standalone expenses.</summary>
    public string? SourceType { get; set; }

    /// <summary>The Id of the owning Transportation/Lodging/Activity record. Null for standalone.</summary>
    public Guid? SourceId { get; set; }

    public string? CreatedById { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<ExpenseSplit> Splits { get; set; } = [];
    public List<Guid> AttachmentIds { get; set; } = [];
}

public class ExpenseSplit
{
    public int TravellerProfileId { get; set; }
    public decimal Amount { get; set; }
}
