using Viagem.Data.Models;
using Viagem.Data.Repositories.Interfaces;
using Viagem.Services.Interfaces;
using Viagem.Services.ViewModels;

namespace Viagem.Services;

public class LodgingService(ILodgingRepository repo) : ILodgingService
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
            CostAmount = request.CostAmount,
            CostCurrency = request.CostCurrency,
            PlaceId = request.PlaceId,
            Travellers = request.TravellerProfileIds
                .Select(id => new LodgingTraveller { TravellerProfileId = id })
                .ToList()
        };

        var created = await repo.CreateAsync(entity);
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
            CostAmount = request.CostAmount,
            CostCurrency = request.CostCurrency,
            PlaceId = request.PlaceId,
            ExpenseId = existing.ExpenseId,
            CreatedAt = existing.CreatedAt
        };

        await repo.UpdateAsync(stub);
        var full = await repo.GetByIdAsync(request.Id);
        return full == null ? null : ToViewModel(full);
    }

    public Task DeleteAsync(int id) => repo.DeleteAsync(id);

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
