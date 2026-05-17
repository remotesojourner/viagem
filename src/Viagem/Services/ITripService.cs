using Viagem.Data.Models;

namespace Viagem.Services;

public interface ITripService
{
    Task<List<Trip>> GetUpcomingTripsAsync(string userId);
    Task<List<Trip>> GetPastTripsAsync(string userId);
    Task<Trip?> GetTripAsync(int tripId, string userId);
    Task<Trip> CreateTripAsync(Trip trip);
    Task<Trip> UpdateTripAsync(Trip trip);
    Task DeleteTripAsync(int tripId, string userId);
    Task<bool> CanUserEditTripAsync(int tripId, string userId);

    // Destinations
    Task<TripDestination> AddDestinationAsync(int tripId, int placeId);
    Task<TripDestination> AddDestinationCustomAsync(int tripId, string customName);
    Task RemoveDestinationAsync(int destinationId);

    // Travellers
    Task AddTravellerAsync(int tripId, int travellerProfileId, bool canEdit = false, bool isOrganiser = false);
    Task RemoveTravellerAsync(int tripTravellerId);
    Task SetTravellerEditAsync(int tripTravellerId, bool canEdit);
    Task SetTravellerOrganiserAsync(int tripTravellerId, bool isOrganiser);

    // Notes
    Task UpdateNotesAsync(int tripId, string? notes);
}

public interface IPlaceService
{
    Task<List<Place>> SearchAsync(string query, int limit = 20);
    Task<Place?> GetByIdAsync(int id);
}

public interface IAirportService
{
    Task<List<Airport>> SearchAsync(string query, int limit = 10);
    Task<Airport?> GetByCodeAsync(string iataCode);
}

public interface IAirlineService
{
    Task<Airline?> GetByCodeAsync(string code);
    Task<List<Airline>> SearchAsync(string query, int limit = 5);
}
