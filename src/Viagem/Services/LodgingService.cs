using Viagem.Data.Models;
using Viagem.Data.Repositories.Interfaces;
using Viagem.Services.Interfaces;
using Viagem.Services.ViewModels;

namespace Viagem.Services;

public class LodgingService(ILodgingRepository repo, IExpenseRepository expenseRepo) : ILodgingService
{
    public async Task<List<LodgingViewModel>> GetTripLodgingsAsync(int tripId)
    {
        var items = await repo.GetByTripAsync(tripId);
        return items.Select(ToViewModel).ToList();
    }

    public async Task<LodgingViewModel?> GetLodgingAsync(int id)
    {
        var item = await repo.GetByIdAsync(id);
        return item == null ? null : ToViewModel(item);
    }

    public async Task<LodgingViewModel> CreateAsync(CreateLodgingRequest request)
    {
        var entity = new Lodging
        {
            TripId = request.TripId,
            Type = request.Type,
            Name = request.Name,
            Address = request.Address,
            ConfirmationCode = request.ConfirmationCode,
            Notes = request.Notes,
            Link = request.Link,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Timezone = request.Timezone,
            PlaceId = request.PlaceId,
            Travellers = request.TravellerProfileIds
                .Select(id => new LodgingTraveller { TravellerProfileId = id })
                .ToList()
        };

        var created = await repo.CreateAsync(entity);

        if (request.CostAmount is > 0 && !string.IsNullOrEmpty(request.CostCurrency))
        {
            var expenseId = await expenseRepo.UpsertLinkedAsync(
                created.TripId, "Lodging", created.Id,
                created.Name,
                ExpenseCategory.Accommodation,
                request.CostAmount.Value, request.CostCurrency,
                created.StartDate, null);
            created.ExpenseId = expenseId;
            await repo.UpdateAsync(created);
        }

        var full = await repo.GetByIdAsync(created.Id);
        return ToViewModel(full!);
    }

    public async Task<LodgingViewModel?> UpdateAsync(UpdateLodgingRequest request)
    {
        var existing = await repo.GetByIdAsync(request.Id);
        if (existing == null) return null;

        var stub = new Lodging
        {
            Id = existing.Id,
            TripId = existing.TripId,
            Type = request.Type,
            Name = request.Name,
            Address = request.Address,
            ConfirmationCode = request.ConfirmationCode,
            Notes = request.Notes,
            Link = request.Link,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Timezone = request.Timezone,
            PlaceId = request.PlaceId,
            ExpenseId = existing.ExpenseId,
            CreatedAt = existing.CreatedAt
        };

        await repo.UpdateAsync(stub);

        // Sync linked expense from request values
        if (request.CostAmount is > 0 && !string.IsNullOrEmpty(request.CostCurrency))
        {
            var expenseId = await expenseRepo.UpsertLinkedAsync(
                existing.TripId, "Lodging", existing.Id,
                request.Name,
                ExpenseCategory.Accommodation,
                request.CostAmount.Value, request.CostCurrency,
                request.StartDate, existing.ExpenseId);
            if (expenseId != existing.ExpenseId)
            {
                stub.ExpenseId = expenseId;
                await repo.UpdateAsync(stub);
            }
        }
        else if (existing.ExpenseId.HasValue)
        {
            await expenseRepo.DeleteLinkedAsync(existing.ExpenseId);
            stub.ExpenseId = null;
            await repo.UpdateAsync(stub);
        }

        var full = await repo.GetByIdAsync(request.Id);
        return full == null ? null : ToViewModel(full);
    }

    public async Task DeleteAsync(int id)
    {
        var existing = await repo.GetByIdAsync(id);
        if (existing?.ExpenseId.HasValue == true)
            await expenseRepo.DeleteLinkedAsync(existing.ExpenseId);
        await repo.DeleteAsync(id);
    }

    // ── Mapping ───────────────────────────────────────────────────────────────

    private static TravellerProfileSummaryViewModel ToTravellerSummary(TravellerProfile tp)
        => new(tp.Id, tp.LegalName, tp.Email);

    private static LodgingViewModel ToViewModel(Lodging l)
        => new(l.Id, l.TripId, l.Type, l.Name, l.Address, l.ConfirmationCode,
            l.Notes, l.Link, l.StartDate, l.EndDate, l.Timezone,
            l.CostAmount, l.CostCurrency, l.PlaceId, l.Place?.Name,
            l.Travellers
                .Where(lt => lt.TravellerProfile != null)
                .Select(lt => ToTravellerSummary(lt.TravellerProfile!))
                .ToList());
}
