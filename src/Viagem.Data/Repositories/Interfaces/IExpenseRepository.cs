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
}
