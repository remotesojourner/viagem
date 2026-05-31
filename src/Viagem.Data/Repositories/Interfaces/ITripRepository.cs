using Viagem.Data.Models;

namespace Viagem.Data.Repositories.Interfaces;

public interface ITripRepository
{
    Task<PagedResult<Trip>> GetUpcomingPagedAsync(string userId, int page, int pageSize, string filter = "all");
    Task<PagedResult<Trip>> GetPastPagedAsync(string userId, int page, int pageSize, string filter = "all");
    Task<Trip?> GetByIdAsync(int tripId, string userId);
    Task<Trip> CreateAsync(Trip trip);
    Task<Trip> UpdateAsync(Trip trip);
    Task<Trip> UpdateWithRelationsAsync(Trip trip, IEnumerable<TripDestination> destinations, IEnumerable<TripTraveller> travellers);
    Task DeleteAsync(int tripId, string userId);
    Task<bool> CanEditAsync(int tripId, string userId);
    Task<TripDestination> AddDestinationAsync(int tripId, int placeId);
    Task<TripDestination> AddDestinationCustomAsync(int tripId, string customName);
    Task RemoveDestinationAsync(int tripId, Guid destinationId);
    Task AddTravellerAsync(int tripId, int travellerProfileId, bool canEdit = false, bool isOrganiser = false);
    Task RemoveTravellerAsync(int tripTravellerId);
    Task SetTravellerEditAsync(int tripTravellerId, bool canEdit);
    Task SetTravellerOrganiserAsync(int tripTravellerId, bool isOrganiser);
    Task UpdateNotesAsync(int tripId, string? notes);
    Task UpdateCoverImageAsync(int tripId, string? coverImagePath);
}
