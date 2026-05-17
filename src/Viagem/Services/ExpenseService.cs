using Microsoft.EntityFrameworkCore;
using Viagem.Data;
using Viagem.Data.Models;

namespace Viagem.Services;

public class ExpenseService(ApplicationDbContext db) : IExpenseService
{
    public async Task<List<Expense>> GetTripExpensesAsync(int tripId)
        => await db.Expenses
            .Include(e => e.Splits).ThenInclude(s => s.TravellerProfile)
            .Include(e => e.CreatedBy)
            .Where(e => e.TripId == tripId)
            .OrderByDescending(e => e.OccurredOn ?? e.CreatedAt)
            .ToListAsync();

    public async Task<Expense?> GetExpenseAsync(int id)
        => await db.Expenses
            .Include(e => e.Splits).ThenInclude(s => s.TravellerProfile)
            .Include(e => e.CreatedBy)
            .Include(e => e.Attachments).ThenInclude(ea => ea.Attachment)
            .FirstOrDefaultAsync(e => e.Id == id);

    public async Task<Expense> CreateAsync(Expense expense)
    {
        expense.CreatedAt = DateTime.UtcNow;
        expense.UpdatedAt = DateTime.UtcNow;
        db.Expenses.Add(expense);
        await db.SaveChangesAsync();
        return expense;
    }

    public async Task<Expense> UpdateAsync(Expense expense)
    {
        expense.UpdatedAt = DateTime.UtcNow;
        var tracked = db.ChangeTracker.Entries<Expense>()
            .FirstOrDefault(e => e.Entity.Id == expense.Id);
        if (tracked != null)
            tracked.State = EntityState.Detached;
        db.Expenses.Update(expense);
        await db.SaveChangesAsync();
        return expense;
    }

    public async Task DeleteAsync(int id)
    {
        var item = await db.Expenses.FindAsync(id);
        if (item != null)
        {
            db.Expenses.Remove(item);
            await db.SaveChangesAsync();
        }
    }

    public async Task<decimal> GetTotalExpenseAsync(int tripId, string currency)
    {
        return await db.Expenses
            .Where(e => e.TripId == tripId && e.Currency == currency)
            .SumAsync(e => e.Amount ?? 0);
    }
}
