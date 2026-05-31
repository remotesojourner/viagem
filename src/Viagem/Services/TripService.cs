using Viagem.Data.Models;
using Viagem.Data.Repositories.Interfaces;
using Viagem.Services.Interfaces;
using Viagem.Services.ViewModels;

namespace Viagem.Services;

public partial class TripService(ITripRepository repo) : ITripService
{
    public async Task<PagedResult<TripSummaryViewModel>> GetUpcomingTripsAsync(string userId, int page, int pageSize, string filter = "all")
    {
        var result = await repo.GetUpcomingPagedAsync(userId, page, pageSize, filter);
        return new PagedResult<TripSummaryViewModel>
        {
            Items = result.Items.Select(r => ToSummaryViewModel(r, userId)).ToList(),
            TotalCount = result.TotalCount
        };
    }

    public async Task<PagedResult<TripSummaryViewModel>> GetPastTripsAsync(string userId, int page, int pageSize, string filter = "all")
    {
        var result = await repo.GetPastPagedAsync(userId, page, pageSize, filter);
        return new PagedResult<TripSummaryViewModel>
        {
            Items = result.Items.Select(r => ToSummaryViewModel(r, userId)).ToList(),
            TotalCount = result.TotalCount
        };
    }

    public async Task<TripDetailViewModel?> GetTripAsync(int tripId, string userId)
    {
        var trip = await repo.GetByIdAsync(tripId, userId);
        return trip == null ? null : ToDetailViewModel(trip, userId);
    }

    public async Task<TripDetailViewModel> CreateTripAsync(CreateTripRequest request)
    {
        var trip = new Trip
        {
            OwnerId = request.OwnerId,
            Name = request.Name,
            Description = request.Description,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            BudgetAmount = request.BudgetAmount,
            BudgetCurrency = request.BudgetCurrency
        };

        foreach (var placeId in request.PlaceIds)
            trip.Destinations.Add(new TripDestination { PlaceId = placeId });
        foreach (var customName in request.CustomDestinationNames)
            trip.Destinations.Add(new TripDestination { CustomName = customName.Trim() });
        foreach (var profileId in request.TravellerProfileIds)
            trip.Travellers.Add(new TripTraveller { TravellerProfileId = profileId });

        var created = await repo.CreateAsync(trip);
        var full = await repo.GetByIdAsync(created.Id, request.OwnerId);
        return ToDetailViewModel(full!, request.OwnerId);
    }

    public async Task<TripDetailViewModel?> UpdateTripAsync(string userId, UpdateTripRequest request)
    {
        var trip = await repo.GetByIdAsync(request.Id, userId);
        if (trip == null) return null;

        trip.Name = request.Name;
        trip.Description = request.Description;
        trip.StartDate = request.StartDate;
        trip.EndDate = request.EndDate;
        trip.BudgetAmount = request.BudgetAmount;
        trip.BudgetCurrency = request.BudgetCurrency;

        var destinations = request.Destinations.Select(d => new TripDestination
        {
            Id = d.Id ?? Guid.NewGuid(),
            PlaceId = d.PlaceId,
            CustomName = d.CustomName
        }).ToList();

        var travellers = request.Travellers.Select(t => new TripTraveller
        {
            TravellerProfileId = t.TravellerProfileId,
            CanEdit = t.CanEdit,
            IsOrganiser = t.IsOrganiser
        }).ToList();

        await repo.UpdateWithRelationsAsync(trip, destinations, travellers);
        var full = await repo.GetByIdAsync(request.Id, userId);
        return full == null ? null : ToDetailViewModel(full, userId);
    }

    public Task DeleteTripAsync(int tripId, string userId)
        => repo.DeleteAsync(tripId, userId);

    public Task<bool> CanUserEditTripAsync(int tripId, string userId)
        => repo.CanEditAsync(tripId, userId);

    public async Task<TripDestinationViewModel> AddDestinationAsync(int tripId, int placeId)
    {
        var dest = await repo.AddDestinationAsync(tripId, placeId);
        return ToDestinationViewModel(dest);
    }

    public async Task<TripDestinationViewModel> AddDestinationCustomAsync(int tripId, string customName)
    {
        var dest = await repo.AddDestinationCustomAsync(tripId, customName);
        return ToDestinationViewModel(dest);
    }

    public Task RemoveDestinationAsync(int tripId, Guid destinationId)
        => repo.RemoveDestinationAsync(tripId, destinationId);

    public Task AddTravellerAsync(int tripId, int travellerProfileId, bool canEdit = false, bool isOrganiser = false)
        => repo.AddTravellerAsync(tripId, travellerProfileId, canEdit, isOrganiser);

    public Task RemoveTravellerAsync(int tripTravellerId)
        => repo.RemoveTravellerAsync(tripTravellerId);

    public Task SetTravellerEditAsync(int tripTravellerId, bool canEdit)
        => repo.SetTravellerEditAsync(tripTravellerId, canEdit);

    public Task SetTravellerOrganiserAsync(int tripTravellerId, bool isOrganiser)
        => repo.SetTravellerOrganiserAsync(tripTravellerId, isOrganiser);

    public Task UpdateNotesAsync(int tripId, string? notes)
        => repo.UpdateNotesAsync(tripId, notes);

    public Task UpdateCoverImageAsync(int tripId, string? coverImagePath)
        => repo.UpdateCoverImageAsync(tripId, coverImagePath);

    // ── Mapping ───────────────────────────────────────────────────────────────

    private static TripDestinationViewModel ToDestinationViewModel(TripDestination d)
        => new(d.Id, d.PlaceId, null, d.CustomName,
            null, null, null,
            null, null);

    private static TripTravellerViewModel ToTravellerViewModel(TripTraveller tt)
        => new(tt.Id, tt.TravellerProfileId,
            tt.TravellerProfile?.LegalName ?? "",
            tt.TravellerProfile?.Email,
            tt.CanEdit, tt.IsOrganiser);

    // Removed TripSummaryRow overload

    private static TripSummaryViewModel ToSummaryViewModel(Trip t, string userId)
        => new(t.Id, t.Name, t.CoverImagePath, t.StartDate, t.EndDate,
            t.Destinations.Select(ToDestinationViewModel).ToList(),
            t.OwnerId == userId,
            t.Travellers.Any(tt => tt.TravellerProfile?.LinkedUserId == userId));

    private static TripDetailViewModel ToDetailViewModel(Trip t, string userId)
        => new(t.Id, t.Name, t.Description, t.Notes, t.CoverImagePath,
            t.StartDate, t.EndDate, t.BudgetAmount, t.BudgetCurrency,
            t.OwnerId == userId ||
                t.Travellers.Any(tt => tt.CanEdit && tt.TravellerProfile?.LinkedUserId == userId),
            t.Destinations.Select(ToDestinationViewModel).ToList(),
            t.Travellers.Select(ToTravellerViewModel).ToList(),
            t.Transportations.Select(tr => ToTransportationViewModel(tr, t)).ToList(),
            t.Lodgings.Select(l => ToLodgingViewModel(l, t)).ToList(),
            t.Activities.Select(a => ToActivityViewModel(a, t)).ToList(),
            t.Expenses.Select(e => ToExpenseViewModel(e, t)).ToList());
}

public class PlaceService(IReferenceDataCache cache) : IPlaceService
{
    public Task<List<PlaceViewModel>> SearchAsync(string query, int limit = 20)
    {
        if (string.IsNullOrWhiteSpace(query)) return Task.FromResult(new List<PlaceViewModel>());
        var lower = query.ToLowerInvariant();
        var results = cache.Places
            .Where(p => p.Name.ToLowerInvariant().Contains(lower) || (p.CountryName != null && p.CountryName.ToLowerInvariant().Contains(lower)))
            .Take(limit)
            .ToList();
        return Task.FromResult(results);
    }

    public Task<PlaceViewModel?> GetByIdAsync(int id)
    {
        var place = cache.Places.FirstOrDefault(p => p.Id == id);
        return Task.FromResult(place);
    }
}

public class AirportService(IReferenceDataCache cache) : IAirportService
{
    public Task<List<AirportViewModel>> SearchAsync(string query, int limit = 10)
    {
        if (string.IsNullOrWhiteSpace(query)) return Task.FromResult(new List<AirportViewModel>());
        var lower = query.ToLowerInvariant();
        var results = cache.Airports
            .Where(a => a.IataCode.ToLowerInvariant().Contains(lower) || a.Name.ToLowerInvariant().Contains(lower) || (a.Municipality != null && a.Municipality.ToLowerInvariant().Contains(lower)))
            .OrderBy(a => a.Name)
            .Take(limit)
            .ToList();
        return Task.FromResult(results);
    }

    public Task<AirportViewModel?> GetByCodeAsync(string iataCode)
    {
        var airport = cache.Airports.FirstOrDefault(a => string.Equals(a.IataCode, iataCode, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(airport);
    }
}

public class AirlineService(IReferenceDataCache cache) : IAirlineService
{
    public Task<AirlineViewModel?> GetByCodeAsync(string code)
    {
        var airline = cache.Airlines.FirstOrDefault(a => string.Equals(a.Code, code, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(airline);
    }

    public Task<List<AirlineViewModel>> SearchAsync(string query, int limit = 5)
    {
        if (string.IsNullOrWhiteSpace(query)) return Task.FromResult(new List<AirlineViewModel>());
        var lower = query.ToLowerInvariant();
        var results = cache.Airlines
            .Where(a => a.Name.ToLowerInvariant().Contains(lower) || a.Code.ToLowerInvariant().Contains(lower))
            .Take(limit)
            .ToList();
        return Task.FromResult(results);
    }
}
