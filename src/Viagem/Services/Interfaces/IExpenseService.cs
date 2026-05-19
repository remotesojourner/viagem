using Viagem.Services.ViewModels;

namespace Viagem.Services.Interfaces;

public interface IExpenseService
{
    Task<List<ExpenseViewModel>> GetTripExpensesAsync(int tripId);
    Task<ExpenseViewModel?> GetExpenseAsync(int id);
    Task<ExpenseViewModel> CreateAsync(CreateExpenseRequest request);
    Task<ExpenseViewModel?> UpdateAsync(UpdateExpenseRequest request);
    Task DeleteAsync(int id);
    Task<decimal> GetTotalExpenseAsync(int tripId, string currency);
}
