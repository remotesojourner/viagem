namespace Viagem.Services.ViewModels;

public record TripDestinationViewModel(
    int Id,
    int? PlaceId,
    string? PlaceName,
    string? CustomName,
    string? PlaceTimezone,
    string? PlaceStateName,
    string? PlaceCountryName,
    string? PlaceLatitude,
    string? PlaceLongitude)
{
    public string DisplayName => PlaceName ?? CustomName ?? "Unknown";
}

public record TripTravellerViewModel(
    int Id,
    int TravellerProfileId,
    string LegalName,
    string? Email,
    bool CanEdit,
    bool IsOrganiser);

public record TripSummaryViewModel(
    int Id,
    string Name,
    string? CoverImagePath,
    DateTime StartDate,
    DateTime EndDate,
    IReadOnlyList<TripDestinationViewModel> Destinations,
    bool CurrentUserIsOwner,
    bool CurrentUserIsTraveller);

public record TripDetailViewModel(
    int Id,
    string Name,
    string? Description,
    string? Notes,
    string? CoverImagePath,
    DateTime StartDate,
    DateTime EndDate,
    decimal? BudgetAmount,
    string? BudgetCurrency,
    bool CurrentUserCanEdit,
    IReadOnlyList<TripDestinationViewModel> Destinations,
    IReadOnlyList<TripTravellerViewModel> Travellers);
