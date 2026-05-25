using Viagem.Data.Models;
using Viagem.Data.Repositories.Interfaces;
using Viagem.Services.Interfaces;
using Viagem.Services.ViewModels;

namespace Viagem.Services;

public class ExpenseService(IExpenseRepository repo) : IExpenseService
{
    public async Task<List<ExpenseViewModel>> GetTripExpensesAsync(int tripId)
    {
        var items = await repo.GetByTripAsync(tripId);
        return items.Select(ToViewModel).ToList();
    }

    public async Task<ExpenseViewModel?> GetExpenseAsync(int id)
    {
        var item = await repo.GetByIdAsync(id);
        return item == null ? null : ToViewModel(item);
    }

    public async Task<ExpenseViewModel> CreateAsync(CreateExpenseRequest request)
    {
        var entity = new Expense
        {
            TripId = request.TripId,
            CreatedById = request.CreatedById,
            Name = request.Name,
            Category = request.Category,
            Notes = request.Notes,
            Amount = request.Amount,
            Currency = request.Currency,
            OccurredOn = request.OccurredOn,
            Splits = request.Splits
                .Select(s => new ExpenseSplit
                {
                    TravellerProfileId = s.TravellerProfileId,
                    Amount = s.Amount
                })
                .ToList()
        };

        var created = await repo.CreateAsync(entity);
        var full = await repo.GetByIdAsync(created.Id);
        return ToViewModel(full!);
    }

    public async Task<ExpenseViewModel?> UpdateAsync(UpdateExpenseRequest request)
    {
        var existing = await repo.GetByIdAsync(request.Id);
        if (existing == null) return null;

        // Update only scalar fields; split management is handled by separate operations
        existing.Name = request.Name;
        existing.Category = request.Category;
        existing.Notes = request.Notes;
        existing.Amount = request.Amount;
        existing.Currency = request.Currency;
        existing.OccurredOn = request.OccurredOn;

        await repo.UpdateAsync(existing);
        var full = await repo.GetByIdAsync(request.Id);
        return full == null ? null : ToViewModel(full);
    }

    public Task DeleteAsync(int id) => repo.DeleteAsync(id);

    public Task<decimal> GetTotalExpenseAsync(int tripId, string currency)
        => repo.GetTotalAsync(tripId, currency);

    // ── Mapping ───────────────────────────────────────────────────────────────

    private static ExpenseSplitViewModel ToSplitViewModel(ExpenseSplit s)
        => new(s.Id, s.TravellerProfileId,
            s.TravellerProfile?.LegalName ?? "",
            s.Amount);

    private static ExpenseViewModel ToViewModel(Expense e)
        => new(e.Id, e.TripId, e.Name, e.Category, e.Notes,
            e.Amount, e.Currency, e.OccurredOn,
            e.CreatedBy?.UserName,
            e.Splits.Select(ToSplitViewModel).ToList(),
            e.SourceType != null,
            e.SourceType,
            e.SourceId);
}
