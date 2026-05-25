using Viagem.Data.Models;

namespace Viagem.Data.Repositories.Interfaces;

public interface IExpenseRepository
{
    Task<List<Expense>> GetByTripAsync(int tripId);
    Task<Expense?> GetByIdAsync(int id);
    Task<Expense> CreateAsync(Expense expense);
    Task<Expense> UpdateAsync(Expense expense);
    Task DeleteAsync(int id);
    Task<decimal> GetTotalAsync(int tripId, string currency);

    /// <summary>
    /// Creates or updates the linked expense for a Transportation/Lodging/Activity record.
    /// Returns the expense Id that should be stored on the parent entity.
    /// </summary>
    Task<int> UpsertLinkedAsync(int tripId, string sourceType, int sourceId,
        string name, ExpenseCategory category, decimal amount, string currency, DateTime occurredOn,
        int? existingExpenseId);

    /// <summary>
    /// Deletes the linked expense for a Transportation/Lodging/Activity record if it exists.
    /// </summary>
    Task DeleteLinkedAsync(int? expenseId);
}
