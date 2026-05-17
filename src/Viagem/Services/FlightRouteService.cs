using System.Text.Json;
using System.Text.Json.Serialization;
using Viagem.Services.Interfaces;

namespace Viagem.Services;

public record FlightRouteResult(
    string? AirlineName,
    string? OriginIataCode,
    string? OriginName,
    string? OriginCity,
    string? DestinationIataCode,
    string? DestinationName,
    string? DestinationCity);

public interface IFlightRouteService
{
    Task<FlightRouteResult?> GetRouteAsync(string flightNumber);
}

public class FlightRouteService(IHttpClientFactory httpClientFactory, IAirportService airportService) : IFlightRouteService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };

    public async Task<FlightRouteResult?> GetRouteAsync(string flightNumber)
    {
        try
        {
            var callsign = flightNumber.Trim().ToUpperInvariant();
            var client = httpClientFactory.CreateClient("adsbdb");
            var response = await client.GetAsync($"https://api.adsbdb.com/v0/callsign/{Uri.EscapeDataString(callsign)}");
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<AdsbRoot>(json, JsonOptions);
            var route = data?.Response?.Flightroute;
            if (route == null) return null;

            // Enrich city names from local airport database (has municipality data)
            var originAirport = !string.IsNullOrEmpty(route.Origin.IataCode)
                ? await airportService.GetByCodeAsync(route.Origin.IataCode)
                : null;
            var destAirport = !string.IsNullOrEmpty(route.Destination.IataCode)
                ? await airportService.GetByCodeAsync(route.Destination.IataCode)
                : null;

            return new FlightRouteResult(
                AirlineName: route.Airline.Name,
                OriginIataCode: route.Origin.IataCode,
                OriginName: originAirport?.Name ?? route.Origin.Name,
                OriginCity: originAirport?.Municipality ?? route.Origin.Municipality,
                DestinationIataCode: route.Destination.IataCode,
                DestinationName: destAirport?.Name ?? route.Destination.Name,
                DestinationCity: destAirport?.Municipality ?? route.Destination.Municipality
            );
        }
        catch
        {
            return null;
        }
    }

    // Private ADSB API response shape (snake_case mapped via JsonNamingPolicy)
    private class AdsbRoot { public AdsbInner? Response { get; set; } }
    private class AdsbInner { public AdsbFlightRoute? Flightroute { get; set; } }
    private class AdsbFlightRoute
    {
        public AdsbAirline Airline { get; set; } = new();
        public AdsbAirport Origin { get; set; } = new();
        public AdsbAirport Destination { get; set; } = new();
    }
    private class AdsbAirline { public string? Name { get; set; } }
    private class AdsbAirport
    {
        public string? IataCode { get; set; }
        public string? Name { get; set; }
        public string? Municipality { get; set; }
    }
}
