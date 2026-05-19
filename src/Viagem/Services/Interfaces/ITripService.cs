using Viagem.Services.ViewModels;

namespace Viagem.Services.Interfaces;

public interface ITripService
{
    Task<List<TripSummaryViewModel>> GetUpcomingTripsAsync(string userId);
    Task<List<TripSummaryViewModel>> GetPastTripsAsync(string userId);
    Task<TripDetailViewModel?> GetTripAsync(int tripId, string userId);
    Task<TripDetailViewModel> CreateTripAsync(CreateTripRequest request);
    Task<TripDetailViewModel?> UpdateTripAsync(string userId, UpdateTripRequest request);
    Task DeleteTripAsync(int tripId, string userId);
    Task<bool> CanUserEditTripAsync(int tripId, string userId);

    // Destinations
    Task<TripDestinationViewModel> AddDestinationAsync(int tripId, int placeId);
    Task<TripDestinationViewModel> AddDestinationCustomAsync(int tripId, string customName);
    Task RemoveDestinationAsync(int destinationId);

    // Travellers
    Task AddTravellerAsync(int tripId, int travellerProfileId, bool canEdit = false, bool isOrganiser = false);
    Task RemoveTravellerAsync(int tripTravellerId);
    Task SetTravellerEditAsync(int tripTravellerId, bool canEdit);
    Task SetTravellerOrganiserAsync(int tripTravellerId, bool isOrganiser);

    // Notes
    Task UpdateNotesAsync(int tripId, string? notes);
    Task UpdateCoverImageAsync(int tripId, string? coverImagePath);
}

public interface IPlaceService
{
    Task<List<PlaceViewModel>> SearchAsync(string query, int limit = 20);
    Task<PlaceViewModel?> GetByIdAsync(int id);
}

public interface IAirportService
{
    Task<List<AirportViewModel>> SearchAsync(string query, int limit = 10);
    Task<AirportViewModel?> GetByCodeAsync(string iataCode);
}

public interface IAirlineService
{
    Task<AirlineViewModel?> GetByCodeAsync(string code);
    Task<List<AirlineViewModel>> SearchAsync(string query, int limit = 5);
}
