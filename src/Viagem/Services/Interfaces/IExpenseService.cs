using Viagem.Data.Models;

namespace Viagem.Services.Interfaces;

public interface IExpenseService
{
    Task<List<Expense>> GetTripExpensesAsync(int tripId);
    Task<Expense?> GetExpenseAsync(int id);
    Task<Expense> CreateAsync(Expense expense);
    Task<Expense> UpdateAsync(Expense expense);
    Task DeleteAsync(int id);
    Task<decimal> GetTotalExpenseAsync(int tripId, string currency);
}
