using Viagem.Data.Models;

namespace Viagem.Services.ViewModels;

public record ExpenseSplitViewModel(
    int TravellerProfileId,
    string TravellerName,
    decimal Amount);

public record ExpenseViewModel(
    Guid Id,
    int TripId,
    string Name,
    ExpenseCategory? Category,
    string? Notes,
    decimal? Amount,
    string? Currency,
    DateTime? OccurredOn,
    string? CreatedByName,
    IReadOnlyList<ExpenseSplitViewModel> Splits,
    bool IsLinked,
    string? SourceType,
    Guid? SourceId);

public class ExpenseSplitFormItem
{
    public int TravellerProfileId { get; set; }
    public string TravellerName { get; set; } = "";
    public decimal Amount { get; set; }
}

public class ExpenseFormModel
{
    public string Name { get; set; } = "";
    public ExpenseCategory? Category { get; set; }
    public string? Notes { get; set; }
    public decimal? Amount { get; set; }
    public string? Currency { get; set; }
    public DateTime? OccurredOn { get; set; }
    public List<ExpenseSplitFormItem> Splits { get; set; } = [];
}

public record ExpenseSummaryViewModel(
    decimal Total,
    string Currency,
    IReadOnlyDictionary<ExpenseCategory, decimal> ByCategory);
