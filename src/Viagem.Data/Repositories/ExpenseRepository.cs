using Microsoft.EntityFrameworkCore;
using Viagem.Data.Models;
using Viagem.Data.Repositories.Interfaces;

namespace Viagem.Data.Repositories;

public class ExpenseRepository(ApplicationDbContext db) : IExpenseRepository
{
    public async Task<List<Expense>> GetByTripAsync(int tripId)
        => await db.Expenses
            .Include(e => e.Splits).ThenInclude(s => s.TravellerProfile)
            .Include(e => e.CreatedBy)
            .Where(e => e.TripId == tripId)
            .OrderByDescending(e => e.OccurredOn ?? e.CreatedAt)
            .ToListAsync();

    public async Task<Expense?> GetByIdAsync(int id)
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

    public async Task<decimal> GetTotalAsync(int tripId, string currency)
        => await db.Expenses
            .Where(e => e.TripId == tripId && e.Currency == currency)
            .SumAsync(e => e.Amount ?? 0);

    public async Task<int> UpsertLinkedAsync(int tripId, string sourceType, int sourceId,
        string name, ExpenseCategory category, decimal amount, string currency, DateTime occurredOn,
        int? existingExpenseId)
    {
        if (existingExpenseId.HasValue)
        {
            var existing = await db.Expenses.FindAsync(existingExpenseId.Value);
            if (existing != null)
            {
                existing.Name = name;
                existing.Category = category;
                existing.Amount = amount;
                existing.Currency = currency;
                existing.OccurredOn = occurredOn;
                existing.UpdatedAt = DateTime.UtcNow;
                await db.SaveChangesAsync();
                return existing.Id;
            }
        }

        var expense = new Expense
        {
            TripId = tripId,
            Name = name,
            Category = category,
            Amount = amount,
            Currency = currency,
            OccurredOn = occurredOn,
            SourceType = sourceType,
            SourceId = sourceId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Expenses.Add(expense);
        await db.SaveChangesAsync();
        return expense.Id;
    }

    public async Task DeleteLinkedAsync(int? expenseId)
    {
        if (expenseId == null) return;
        var item = await db.Expenses.FindAsync(expenseId.Value);
        if (item != null)
        {
            db.Expenses.Remove(item);
            await db.SaveChangesAsync();
        }
    }
}
