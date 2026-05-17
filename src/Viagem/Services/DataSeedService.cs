using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Viagem.Data;
using Viagem.Data.Models;

namespace Viagem.Services;

public class DataSeedService(
    ApplicationDbContext db,
    IWebHostEnvironment env,
    IHttpClientFactory httpClientFactory,
    ILogger<DataSeedService> logger)
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    // Download sources (from README credits)
    private const string AirportsCsvUrl = "https://davidmegginson.github.io/ourairports-data/airports.csv";
    private const string AirlinesJsonUrl = "https://raw.githubusercontent.com/dotmarn/Airlines/refs/heads/master/airlines.json";
    private const string PlacesJsonUrl = "https://raw.githubusercontent.com/dr5hn/countries-states-cities-database/refs/heads/master/json/countries%2Bstates%2Bcities.json";

    // snake_case options for dr5hn cities.json (state_name, country_name, etc.)
    private static readonly JsonSerializerOptions SnakeJsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    private string SeedPath(string file) =>
        Path.Combine(env.ContentRootPath, "Data", "Seed", file);

    public async Task SeedAsync()
    {
        if (!await db.Airports.AnyAsync()) await LoadAirportsAsync();
        if (!await db.Airlines.AnyAsync()) await LoadAirlinesAsync();
        if (!await db.Places.AnyAsync()) await LoadPlacesAsync();
    }

    // Public methods callable from Settings UI
    public Task<int> CountAirportsAsync() => db.Airports.CountAsync();
    public Task<int> CountAirlinesAsync() => db.Airlines.CountAsync();
    public Task<int> CountPlacesAsync() => db.Places.CountAsync();

    public async Task<int> LoadAirportsAsync()
    {
        await db.Airports.ExecuteDeleteAsync();

        List<Airport> airports;
        var path = SeedPath("airports.json");

        if (File.Exists(path))
        {
            airports = await LoadAirportsFromJsonAsync(path);
        }
        else
        {
            logger.LogInformation("airports.json not found locally, downloading from OurAirports...");
            airports = await DownloadAirportsFromCsvAsync();
        }

        if (airports.Count == 0) return 0;
        await db.Airports.AddRangeAsync(airports);
        await db.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} airports", airports.Count);
        return airports.Count;
    }

    public async Task<int> LoadAirlinesAsync()
    {
        await db.Airlines.ExecuteDeleteAsync();

        List<Airline> airlines;
        var path = SeedPath("airlines.json");

        if (File.Exists(path))
        {
            airlines = await LoadAirlinesFromJsonAsync(path);
        }
        else
        {
            logger.LogInformation("airlines.json not found locally, downloading from dotmarn/Airlines...");
            airlines = await DownloadAirlinesFromJsonAsync();
        }

        if (airlines.Count == 0) return 0;
        await db.Airlines.AddRangeAsync(airlines);
        await db.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} airlines", airlines.Count);
        return airlines.Count;
    }

    public async Task<int> LoadPlacesAsync()
    {
        // Remove duplicate rows from previous bad runs before upserting
        await PurgeDuplicatePlacesAsync();

        var citiesPath = SeedPath("cities-major.json");
        if (File.Exists(citiesPath))
        {
            return await SeedPlacesFromCitiesAsync(citiesPath);
        }

        logger.LogInformation("cities-major.json not found locally, downloading from dr5hn/countries-states-cities-database...");
        return await DownloadPlacesFromJsonAsync();
    }

    // Removes duplicate Place rows, keeping the lowest Id in each group (preserves oldest FK references)
    private async Task PurgeDuplicatePlacesAsync()
    {
        var duplicateIds = (await db.Places.ToListAsync())
            .GroupBy(p => $"{p.Name}|{p.StateName}|{p.CountryName}")
            .Where(g => g.Count() > 1)
            .SelectMany(g => g.OrderBy(p => p.Id).Skip(1))
            .Select(p => p.Id)
            .ToList();

        if (duplicateIds.Count == 0) return;

        await db.Places.Where(p => duplicateIds.Contains(p.Id)).ExecuteDeleteAsync();
        logger.LogInformation("Purged {Count} duplicate place rows", duplicateIds.Count);
    }

    // ------------------------------------------------------------------
    // Local JSON loaders
    // ------------------------------------------------------------------

    private static async Task<List<Airport>> LoadAirportsFromJsonAsync(string path)
    {
        await using var stream = File.OpenRead(path);
        var entries = await JsonSerializer.DeserializeAsync<List<AirportSeedEntry>>(stream, JsonOpts);
        if (entries is null) return [];

        return entries
            .Where(e => !string.IsNullOrWhiteSpace(e.IataCode))
            .Select(e => new Airport
            {
                IataCode = e.IataCode!,
                IcaoCode = e.IcaoCode,
                Name = e.Name ?? "",
                Municipality = e.Municipality,
                IsoCountry = e.IsoCountry,
                Type = e.Type,
                Latitude = e.Latitude,
                Longitude = e.Longitude
            }).ToList();
    }

    private async Task<List<Airline>> LoadAirlinesFromJsonAsync(string path)
    {
        await using var stream = File.OpenRead(path);
        var entries = await JsonSerializer.DeserializeAsync<List<AirlineSeedEntry>>(stream, JsonOpts);
        if (entries is null) return [];

        return entries
            .Where(e => !string.IsNullOrEmpty(e.Id))
            .Select(e => new Airline
            {
                Code = e.Id!,
                Name = e.Name ?? "",
                LogoUrl = string.IsNullOrEmpty(e.Logo) ? null : e.Logo
            }).ToList();
    }

    // ------------------------------------------------------------------
    // Internet downloaders
    // ------------------------------------------------------------------

    private async Task<List<Airport>> DownloadAirportsFromCsvAsync()
    {
        // OurAirports CSV columns (0-indexed):
        // 0=id, 1=ident(ICAO), 2=type, 3=name, 4=latitude_deg, 5=longitude_deg,
        // 6=elevation_ft, 7=continent, 8=iso_country, 9=iso_region,
        // 10=municipality, 11=scheduled_service, 12=gps_code, 13=iata_code
        var airports = new List<Airport>();
        try
        {
            using var http = httpClientFactory.CreateClient("seed");
            using var response = await http.GetAsync(AirportsCsvUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            await using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);

            await reader.ReadLineAsync(); // skip header row
            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                var fields = ParseCsvLine(line);
                if (fields.Length < 14) continue;
                var iataCode = fields[13].Trim();
                if (string.IsNullOrWhiteSpace(iataCode)) continue;

                double.TryParse(fields[4], NumberStyles.Float, CultureInfo.InvariantCulture, out double lat);
                double.TryParse(fields[5], NumberStyles.Float, CultureInfo.InvariantCulture, out double lon);

                airports.Add(new Airport
                {
                    IataCode = iataCode,
                    IcaoCode = NullIfEmpty(fields[1]),
                    Name = fields[3],
                    Municipality = NullIfEmpty(fields[10]),
                    IsoCountry = NullIfEmpty(fields[8]),
                    Type = NullIfEmpty(fields[2]),
                    Latitude = lat == 0 ? null : lat,
                    Longitude = lon == 0 ? null : lon
                });
            }

            logger.LogInformation("Downloaded {Count} IATA airports from OurAirports", airports.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to download airports CSV from {Url}", AirportsCsvUrl);
        }
        return airports;
    }

    private async Task<List<Airline>> DownloadAirlinesFromJsonAsync()
    {
        try
        {
            using var http = httpClientFactory.CreateClient("seed");
            await using var stream = await http.GetStreamAsync(AirlinesJsonUrl);
            var entries = await JsonSerializer.DeserializeAsync<List<AirlineSeedEntry>>(stream, JsonOpts);
            if (entries is null) return [];

            var airlines = entries
                .Where(e => !string.IsNullOrEmpty(e.Id))
                .Select(e => new Airline
                {
                    Code = e.Id!,
                    Name = e.Name ?? "",
                    LogoUrl = string.IsNullOrEmpty(e.Logo) ? null : e.Logo
                }).ToList();

            logger.LogInformation("Downloaded {Count} airlines from dotmarn/Airlines", airlines.Count);
            return airlines;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to download airlines JSON from {Url}", AirlinesJsonUrl);
            return [];
        }
    }

    // ------------------------------------------------------------------
    // Places seeding
    // ------------------------------------------------------------------

    private async Task<int> DownloadPlacesFromJsonAsync()
    {
        try
        {
            using var http = httpClientFactory.CreateClient("seed");
            await using var stream = await http.GetStreamAsync(PlacesJsonUrl);
            var countries = await JsonSerializer.DeserializeAsync<List<CountryEntry>>(stream, SnakeJsonOpts);
            if (countries is null || countries.Count == 0) return 0;

            var incoming = new List<Place>();
                foreach (var country in countries)
                {
                    if (country.States is null) continue;
                    foreach (var state in country.States)
                    {
                        if (state.Cities is null) continue;
                        foreach (var city in state.Cities)
                        {
                            incoming.Add(new Place
                            {
                                Name = city.Name ?? "",
                                StateName = state.Name,
                                StateCode = state.Iso2,
                                CountryName = country.Name,
                                CountryCode = country.Iso2,
                                Latitude = city.Latitude,
                                Longitude = city.Longitude,
                                Timezone = city.Timezone
                            });
                        }
                    }
                }

            var count = await UpsertPlacesAsync(incoming);
            logger.LogInformation("Downloaded and seeded {Count} places from dr5hn/countries-states-cities-database", count);
            return count;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to download places JSON from {Url}", PlacesJsonUrl);
            return await SeedPlacesFromAirportsAsync();
        }
    }

    private async Task<int> SeedPlacesFromCitiesAsync(string path)
    {
        await using var stream = File.OpenRead(path);
        var entries = await JsonSerializer.DeserializeAsync<List<CitySeedEntry>>(stream, JsonOpts);
        if (entries is null || entries.Count == 0) return 0;

        var incoming = entries.Select(e => new Place
        {
            Name = e.Name ?? "",
            StateName = e.StateName,
            StateCode = e.StateCode,
            CountryName = e.CountryName,
            CountryCode = e.CountryCode,
            Latitude = e.Latitude,
            Longitude = e.Longitude,
            Timezone = e.Timezone
        }).ToList();

        return await UpsertPlacesAsync(incoming);
    }

    // Upsert: match on Name+StateName+CountryName to preserve existing IDs (FK-safe).
    // New records are inserted; existing records have their Lat/Lng/Timezone updated.
    private async Task<int> UpsertPlacesAsync(List<Place> incoming)
    {
        // Use GroupBy to handle any duplicate rows already in the DB (e.g. from previous bad runs)
        var existing = (await db.Places.ToListAsync())
            .GroupBy(p => $"{p.Name}|{p.StateName}|{p.CountryName}")
            .ToDictionary(g => g.Key, g => g.First());

        // Also deduplicate the incoming list itself (source data can have duplicate city names per state)
        var seen = new HashSet<string>();
        var toInsert = new List<Place>();
        foreach (var place in incoming)
        {
            var key = $"{place.Name}|{place.StateName}|{place.CountryName}";
            if (existing.TryGetValue(key, out var current))
            {
                current.Latitude = place.Latitude;
                current.Longitude = place.Longitude;
                current.Timezone = place.Timezone;
                current.StateCode = place.StateCode;
                current.CountryCode = place.CountryCode;
            }
            else if (seen.Add(key))
            {
                toInsert.Add(place);
            }
        }

        if (toInsert.Count > 0)
            await db.Places.AddRangeAsync(toInsert);

        await db.SaveChangesAsync();
        var total = await db.Places.CountAsync();
        logger.LogInformation("Upserted places: {Updated} updated, {Inserted} inserted, {Total} total",
            incoming.Count - toInsert.Count, toInsert.Count, total);
        return total;
    }

    private async Task<int> SeedPlacesFromAirportsAsync()
    {
        // Build unique city list from airports (municipality + country)
        var airports = await db.Airports.ToListAsync();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var places = new List<Place>();

        foreach (var a in airports.Where(a => !string.IsNullOrEmpty(a.Municipality)))
        {
            var key = $"{a.Municipality}|{a.IsoCountry}";
            if (!seen.Add(key)) continue;

            places.Add(new Place
            {
                Name = a.Municipality!,
                CountryName = a.IsoCountry,
                Latitude = a.Latitude?.ToString(),
                Longitude = a.Longitude?.ToString()
            });
        }

        await db.Places.AddRangeAsync(places);
        await db.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} places from airports", places.Count);
        return places.Count;
    }

    // ------------------------------------------------------------------
    // CSV helpers
    // ------------------------------------------------------------------

    private static string[] ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var current = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                { current.Append('"'); i++; }
                else { inQuotes = !inQuotes; }
            }
            else if (c == ',' && !inQuotes)
            { fields.Add(current.ToString()); current.Clear(); }
            else
            { current.Append(c); }
        }
        fields.Add(current.ToString());
        return [.. fields];
    }

    private static string? NullIfEmpty(string? s) =>
        string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    // Seed DTOs
    private record AirportSeedEntry(
        string? IataCode, string? IcaoCode, string? Name,
        string? Municipality, string? IsoCountry, string? Type,
        double? Latitude, double? Longitude);

    private record AirlineSeedEntry(string? Id, string? Name, string? Logo, string? Lcc);

    // Flat city DTO — used by local cities-major.json seed file
    private record CitySeedEntry(
        int? Id, string? Name, string? StateName, string? CountryName,
        string? StateCode, string? CountryCode,
        string? Latitude, string? Longitude, string? Timezone);

    // Nested DTOs for countries+states+cities.json (dr5hn)
    private record CountryEntry(string? Name, string? Iso2, List<StateEntry>? States);
    private record StateEntry(string? Name, string? Iso2, List<CityEntry>? Cities);
    private record CityEntry(string? Name, string? Latitude, string? Longitude, string? Timezone);
}

