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
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = "";

    public ExpenseCategory? Category { get; set; }
    public string? Notes { get; set; }

    public decimal? Amount { get; set; }
    public string? Currency { get; set; }

    public DateTime? OccurredOn { get; set; }

    public int TripId { get; set; }
    public Trip? Trip { get; set; }

    /// <summary>"Transportation", "Lodging", "Activity", or null for standalone expenses.</summary>
    public string? SourceType { get; set; }

    /// <summary>The Id of the owning Transportation/Lodging/Activity record. Null for standalone.</summary>
    public int? SourceId { get; set; }

    public string? CreatedById { get; set; }
    public ApplicationUser? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ExpenseSplit> Splits { get; set; } = [];
    public ICollection<ExpenseAttachment> Attachments { get; set; } = [];
}

public class ExpenseSplit
{
    public int Id { get; set; }
    public int ExpenseId { get; set; }
    public Expense? Expense { get; set; }
    public int TravellerProfileId { get; set; }
    public TravellerProfile? TravellerProfile { get; set; }
    public decimal Amount { get; set; }
}

public class ExpenseAttachment
{
    public int Id { get; set; }
    public int ExpenseId { get; set; }
    public Expense? Expense { get; set; }
    public int AttachmentId { get; set; }
    public TripAttachment? Attachment { get; set; }
}
