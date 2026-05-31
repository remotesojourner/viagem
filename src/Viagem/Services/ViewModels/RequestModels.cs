using Viagem.Data.Models;

namespace Viagem.Services.ViewModels;

// ── Trip ──────────────────────────────────────────────────────────────────────

public record CreateTripRequest(
    string OwnerId,
    string Name,
    string? Description,
    DateTime StartDate,
    DateTime EndDate,
    decimal? BudgetAmount,
    string? BudgetCurrency,
    IReadOnlyList<int> PlaceIds,
    IReadOnlyList<string> CustomDestinationNames,
    IReadOnlyList<int> TravellerProfileIds);

public record UpdateTripDestinationRequest(Guid? Id, int? PlaceId, string? CustomName);
public record UpdateTripTravellerRequest(int TravellerProfileId, bool CanEdit, bool IsOrganiser);

public record UpdateTripRequest(
    int Id,
    string Name,
    string? Description,
    DateTime StartDate,
    DateTime EndDate,
    decimal? BudgetAmount,
    string? BudgetCurrency,
    IReadOnlyList<UpdateTripDestinationRequest> Destinations,
    IReadOnlyList<UpdateTripTravellerRequest> Travellers);

// ── Activity ──────────────────────────────────────────────────────────────────

public record CreateActivityRequest(
    int TripId,
    string Name,
    string? Description,
    string? Address,
    string? Notes,
    string? Link,
    DateTime StartDate,
    DateTime? EndDate,
    string? Timezone,
    decimal? CostAmount,
    string? CostCurrency,
    int? PlaceId,
    IReadOnlyList<int> TravellerProfileIds);

public record UpdateActivityRequest(
    int TripId,
    Guid Id,
    string Name,
    string? Description,
    string? Address,
    string? Notes,
    string? Link,
    DateTime StartDate,
    DateTime? EndDate,
    string? Timezone,
    decimal? CostAmount,
    string? CostCurrency,
    int? PlaceId,
    IReadOnlyList<int> TravellerProfileIds);

// ── Lodging ───────────────────────────────────────────────────────────────────

public record CreateLodgingRequest(
    int TripId,
    LodgingType Type,
    string Name,
    string? Address,
    string? ConfirmationCode,
    string? Notes,
    string? Link,
    DateTime StartDate,
    DateTime EndDate,
    string? Timezone,
    decimal? CostAmount,
    string? CostCurrency,
    int? PlaceId,
    IReadOnlyList<int> TravellerProfileIds);

public record UpdateLodgingRequest(
    int TripId,
    Guid Id,
    LodgingType Type,
    string Name,
    string? Address,
    string? ConfirmationCode,
    string? Notes,
    string? Link,
    DateTime StartDate,
    DateTime EndDate,
    string? Timezone,
    decimal? CostAmount,
    string? CostCurrency,
    int? PlaceId,
    IReadOnlyList<int> TravellerProfileIds);

// ── Transportation ────────────────────────────────────────────────────────────

public record CreateTransportationRequest(
    int TripId,
    TransportationType Type,
    string? Origin,
    string? OriginCity,
    string? Destination,
    string? DestinationCity,
    string? Provider,
    string? ConfirmationCode,
    string? FlightNumber,
    string? AssignedSeats,
    string? Notes,
    string? Link,
    DateTime DepartureTime,
    DateTime ArrivalTime,
    string? DepartureTimezone,
    string? ArrivalTimezone,
    decimal? CostAmount,
    string? CostCurrency,
    string? RentalCompany,
    string? PickupLocation,
    string? DropOffLocation,
    string? SpotNumber,
    string? ParkingAddress,
    int? OriginPlaceId,
    int? DestinationPlaceId,
    IReadOnlyList<int> TravellerProfileIds);

public record UpdateTransportationRequest(
    int TripId,
    Guid Id,
    TransportationType Type,
    string? Origin,
    string? OriginCity,
    string? Destination,
    string? DestinationCity,
    string? Provider,
    string? ConfirmationCode,
    string? FlightNumber,
    string? AssignedSeats,
    string? Notes,
    string? Link,
    DateTime DepartureTime,
    DateTime ArrivalTime,
    string? DepartureTimezone,
    string? ArrivalTimezone,
    decimal? CostAmount,
    string? CostCurrency,
    string? RentalCompany,
    string? PickupLocation,
    string? DropOffLocation,
    string? SpotNumber,
    string? ParkingAddress,
    int? OriginPlaceId,
    int? DestinationPlaceId,
    IReadOnlyList<int> TravellerProfileIds);

// ── Expense ───────────────────────────────────────────────────────────────────

public record ExpenseSplitRequest(int TravellerProfileId, decimal Amount);

public record CreateExpenseRequest(
    int TripId,
    string? CreatedById,
    string Name,
    ExpenseCategory? Category,
    string? Notes,
    decimal? Amount,
    string? Currency,
    DateTime? OccurredOn,
    IReadOnlyList<ExpenseSplitRequest> Splits);

public record UpdateExpenseRequest(
    int TripId,
    Guid Id,
    string Name,
    ExpenseCategory? Category,
    string? Notes,
    decimal? Amount,
    string? Currency,
    DateTime? OccurredOn,
    IReadOnlyList<ExpenseSplitRequest> Splits);

// ── Traveller Profile ─────────────────────────────────────────────────────────

public record CreateTravellerProfileRequest(
    string OwnerId,
    string LegalName,
    string? Email);

public record UpdateTravellerProfileRequest(
    int Id,
    string LegalName,
    string? Email);
