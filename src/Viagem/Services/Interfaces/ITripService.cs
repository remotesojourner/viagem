using Viagem.Data.Models;
using Viagem.Services.ViewModels;

namespace Viagem.Services.Interfaces;

public interface ITripService
{
    Task<PagedResult<TripSummaryViewModel>> GetUpcomingTripsAsync(string userId, int page, int pageSize, string filter = "all");
    Task<PagedResult<TripSummaryViewModel>> GetPastTripsAsync(string userId, int page, int pageSize, string filter = "all");
    Task<TripDetailViewModel?> GetTripAsync(int tripId, string userId);
    Task<TripDetailViewModel> CreateTripAsync(CreateTripRequest request);
    Task<TripDetailViewModel?> UpdateTripAsync(string userId, UpdateTripRequest request);
    Task DeleteTripAsync(int tripId, string userId);
    Task<bool> CanUserEditTripAsync(int tripId, string userId);

    // Destinations
    Task<TripDestinationViewModel> AddDestinationAsync(int tripId, int placeId);
    Task<TripDestinationViewModel> AddDestinationCustomAsync(int tripId, string customName);
    Task RemoveDestinationAsync(int tripId, Guid destinationId);

    // Travellers
    Task AddTravellerAsync(int tripId, int travellerProfileId, bool canEdit = false, bool isOrganiser = false);
    Task RemoveTravellerAsync(int tripTravellerId);
    Task SetTravellerEditAsync(int tripTravellerId, bool canEdit);
    Task SetTravellerOrganiserAsync(int tripTravellerId, bool isOrganiser);

    // Transportations
    Task<TransportationViewModel> AddTransportationAsync(string userId, CreateTransportationRequest request);
    Task<TransportationViewModel?> UpdateTransportationAsync(string userId, UpdateTransportationRequest request);
    Task RemoveTransportationAsync(string userId, int tripId, Guid id);

    // Lodgings
    Task<LodgingViewModel> AddLodgingAsync(string userId, CreateLodgingRequest request);
    Task<LodgingViewModel?> UpdateLodgingAsync(string userId, UpdateLodgingRequest request);
    Task RemoveLodgingAsync(string userId, int tripId, Guid id);

    // Activities
    Task<ActivityViewModel> AddActivityAsync(string userId, CreateActivityRequest request);
    Task<ActivityViewModel?> UpdateActivityAsync(string userId, UpdateActivityRequest request);
    Task RemoveActivityAsync(string userId, int tripId, Guid id);

    // Expenses
    Task<ExpenseViewModel> AddExpenseAsync(string userId, CreateExpenseRequest request);
    Task<ExpenseViewModel?> UpdateExpenseAsync(string userId, UpdateExpenseRequest request);
    Task RemoveExpenseAsync(string userId, int tripId, Guid id);

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
