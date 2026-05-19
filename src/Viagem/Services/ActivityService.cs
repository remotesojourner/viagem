using Viagem.Data.Models;
using Viagem.Data.Repositories.Interfaces;
using Viagem.Services.Interfaces;
using Viagem.Services.ViewModels;

namespace Viagem.Services;

public class ActivityService(IActivityRepository repo) : IActivityService
{
    public async Task<List<ActivityViewModel>> GetTripActivitiesAsync(int tripId)
    {
        var items = await repo.GetByTripAsync(tripId);
        return items.Select(ToViewModel).ToList();
    }

    public async Task<ActivityViewModel?> GetActivityAsync(int id)
    {
        var item = await repo.GetByIdAsync(id);
        return item == null ? null : ToViewModel(item);
    }

    public async Task<ActivityViewModel> CreateAsync(CreateActivityRequest request)
    {
        var entity = new Activity
        {
            TripId = request.TripId,
            Name = request.Name,
            Description = request.Description,
            Address = request.Address,
            Notes = request.Notes,
            Link = request.Link,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Timezone = request.Timezone,
            CostAmount = request.CostAmount,
            CostCurrency = request.CostCurrency,
            PlaceId = request.PlaceId,
            Travellers = request.TravellerProfileIds
                .Select(id => new ActivityTraveller { TravellerProfileId = id })
                .ToList()
        };

        var created = await repo.CreateAsync(entity);
        var full = await repo.GetByIdAsync(created.Id);
        return ToViewModel(full!);
    }

    public async Task<ActivityViewModel?> UpdateAsync(UpdateActivityRequest request)
    {
        var existing = await repo.GetByIdAsync(request.Id);
        if (existing == null) return null;

        // Build a stub with only scalar fields to avoid graph traversal issues
        var stub = new Activity
        {
            Id = existing.Id,
            TripId = existing.TripId,
            Name = request.Name,
            Description = request.Description,
            Address = request.Address,
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

    private static ActivityViewModel ToViewModel(Activity a)
        => new(a.Id, a.TripId, a.Name, a.Description, a.Address, a.Notes, a.Link,
            a.StartDate, a.EndDate, a.Timezone, a.CostAmount, a.CostCurrency,
            a.PlaceId, a.Place?.Name,
            a.Travellers
                .Where(at => at.TravellerProfile != null)
                .Select(at => ToTravellerSummary(at.TravellerProfile!))
                .ToList());
}
