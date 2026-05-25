using Viagem.Data.Models;
using Viagem.Data.Repositories.Interfaces;
using Viagem.Services.Interfaces;
using Viagem.Services.ViewModels;

namespace Viagem.Services;

public class TripService(ITripRepository repo) : ITripService
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

        await repo.UpdateAsync(trip);
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

    public Task RemoveDestinationAsync(int destinationId)
        => repo.RemoveDestinationAsync(destinationId);

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
        => new(d.Id, d.PlaceId, d.Place?.Name, d.CustomName,
            d.Place?.Timezone, d.Place?.StateName, d.Place?.CountryName,
            d.Place?.Latitude, d.Place?.Longitude);

    private static TripTravellerViewModel ToTravellerViewModel(TripTraveller tt)
        => new(tt.Id, tt.TravellerProfileId,
            tt.TravellerProfile?.LegalName ?? "",
            tt.TravellerProfile?.Email,
            tt.CanEdit, tt.IsOrganiser);

    private static TripSummaryViewModel ToSummaryViewModel(TripSummaryRow r, string userId)
        => new(r.Id, r.Name, r.CoverImagePath, r.StartDate.GetValueOrDefault(), r.EndDate.GetValueOrDefault(),
            r.DestinationNames.Select(n => new TripDestinationViewModel(0, null, n, null, null, null, null, null, null)).ToList(),
            r.OwnerId == userId,
            r.CurrentUserIsTraveller);

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
            t.Travellers.Select(ToTravellerViewModel).ToList());
}

public class PlaceService(IPlaceRepository repo) : IPlaceService
{
    public async Task<List<PlaceViewModel>> SearchAsync(string query, int limit = 20)
    {
        var places = await repo.SearchAsync(query, limit);
        return places.Select(ToViewModel).ToList();
    }

    public async Task<PlaceViewModel?> GetByIdAsync(int id)
    {
        var place = await repo.GetByIdAsync(id);
        return place == null ? null : ToViewModel(place);
    }

    private static PlaceViewModel ToViewModel(Place p)
        => new(p.Id, p.Name, p.StateName, p.CountryName, p.CountryCode, p.Timezone);
}

public class AirportService(IAirportRepository repo) : IAirportService
{
    public async Task<List<AirportViewModel>> SearchAsync(string query, int limit = 10)
    {
        var airports = await repo.SearchAsync(query, limit);
        return airports.Select(ToViewModel).ToList();
    }

    public async Task<AirportViewModel?> GetByCodeAsync(string iataCode)
    {
        var airport = await repo.GetByCodeAsync(iataCode);
        return airport == null ? null : ToViewModel(airport);
    }

    private static AirportViewModel ToViewModel(Airport a)
        => new(a.Id, a.IataCode, a.Name, a.Municipality, a.IsoCountry, a.Latitude, a.Longitude);
}

public class AirlineService(IAirlineRepository repo) : IAirlineService
{
    public async Task<AirlineViewModel?> GetByCodeAsync(string code)
    {
        var airline = await repo.GetByCodeAsync(code);
        return airline == null ? null : ToViewModel(airline);
    }

    public async Task<List<AirlineViewModel>> SearchAsync(string query, int limit = 5)
    {
        var airlines = await repo.SearchAsync(query, limit);
        return airlines.Select(ToViewModel).ToList();
    }

    private static AirlineViewModel ToViewModel(Airline a)
        => new(a.Id, a.Code, a.Name, a.LogoUrl);
}
