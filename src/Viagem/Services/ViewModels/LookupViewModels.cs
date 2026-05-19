namespace Viagem.Services.ViewModels;

public record PlaceViewModel(
    int Id,
    string Name,
    string? StateName,
    string? CountryName,
    string? CountryCode,
    string? Timezone)
{
    public string DisplayName => CountryName != null ? $"{Name}, {CountryName}" : Name;
}

public record AirportViewModel(
    int Id,
    string IataCode,
    string Name,
    string? Municipality,
    string? IsoCountry,
    double? Latitude,
    double? Longitude)
{
    public string DisplayName => $"{IataCode} – {Name}{(Municipality != null ? $", {Municipality}" : "")}";
}

public record AirlineViewModel(
    int Id,
    string Code,
    string Name,
    string? LogoUrl);
