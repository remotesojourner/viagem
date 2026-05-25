using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Viagem.Data;
using Viagem.Data.Models;
using Viagem.Data.Repositories.Interfaces;

namespace Viagem.Services;

/// <summary>
/// Parses a TripIt JSON export file (the full account export, not API JSON) and bulk-imports
/// all trips into Viagem, creating missing traveller profiles as needed.
/// </summary>
public class TripItImportService(
    IDbContextFactory<ApplicationDbContext> dbFactory,
    IExpenseRepository expenseRepo)
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public async Task<List<ImportResult>> ImportAsync(Stream jsonStream, string ownerId)
    {
        var results = new List<ImportResult>();

        JsonNode root;
        try
        {
            root = await JsonNode.ParseAsync(jsonStream)
                   ?? throw new InvalidOperationException("Empty TripIt JSON file.");
        }
        catch (Exception ex)
        {
            results.Add(new ImportResult { Success = false, Error = $"Failed to parse JSON: {ex.Message}" });
            return results;
        }

        var trips = root["Trips"]?.AsArray();
        if (trips == null || trips.Count == 0)
        {
            results.Add(new ImportResult { Success = false, Error = "No trips found in TripIt file." });
            return results;
        }

        // Profile of the TripIt account owner
        var ownerFirstName = root["first_name"]?.GetValue<string>() ?? "";
        var ownerLastName = root["last_name"]?.GetValue<string>() ?? "";
        var ownerLegalName = $"{ownerFirstName} {ownerLastName}".Trim();

        await using var db = await dbFactory.CreateDbContextAsync();

        foreach (var tripNode in trips)
        {
            if (tripNode == null) continue;
            var result = await ImportOneTripAsync(db, tripNode, ownerId, ownerLegalName);
            results.Add(result);
        }

        return results;
    }

    private async Task<ImportResult> ImportOneTripAsync(
        ApplicationDbContext db, JsonNode tripNode, string ownerId, string ownerLegalName)
    {
        var result = new ImportResult();
        try
        {
            var tripData = tripNode["TripData"];
            if (tripData == null)
                return new ImportResult { Success = false, Error = "Trip has no TripData." };

            var startDateStr = tripData["start_date"]?.GetValue<string>();
            var endDateStr = tripData["end_date"]?.GetValue<string>();
            var displayName = tripData["display_name"]?.GetValue<string>() ?? "Imported TripIt Trip";
            var primaryLocation = tripData["primary_location"]?.GetValue<string>();

            if (!DateTime.TryParse(startDateStr, out var startDate))
                startDate = DateTime.UtcNow.Date;
            if (!DateTime.TryParse(endDateStr, out var endDate))
                endDate = startDate;

            result.TripName = displayName;

            var trip = new Trip
            {
                OwnerId = ownerId,
                Name = displayName,
                StartDate = startDate,
                EndDate = endDate,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Add primary location as custom destination
            if (!string.IsNullOrWhiteSpace(primaryLocation))
                trip.Destinations.Add(new TripDestination { CustomName = primaryLocation });

            // Collect all unique traveller names from the objects
            var travellerNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            travellerNames.Add(ownerLegalName);

            var objects = tripNode["Objects"]?.AsArray() ?? [];
            foreach (var obj in objects)
            {
                if (obj == null) continue;
                CollectTravellerNames(obj, travellerNames);
            }

            // Resolve / create traveller profiles
            var profileMap = await ResolveTravellerProfilesAsync(db, travellerNames, ownerId, ownerLegalName, result);

            // Add owner as organiser traveller
            if (profileMap.TryGetValue(ownerLegalName, out var ownerProfileId))
                trip.Travellers.Add(new TripTraveller
                {
                    TravellerProfileId = ownerProfileId,
                    IsOrganiser = true,
                    CanEdit = true
                });

            db.Trips.Add(trip);
            await db.SaveChangesAsync();

            // Process each object
            foreach (var obj in objects)
            {
                if (obj == null) continue;
                await ProcessObjectAsync(db, obj, trip, profileMap, result);
            }

            result.Success = true;
            result.TripId = trip.Id;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Error = ex.Message;
        }

        return result;
    }

    private async Task ProcessObjectAsync(
        ApplicationDbContext db, JsonNode obj, Trip trip,
        Dictionary<string, int> profileMap, ImportResult result)
    {
        var displayName = obj["display_name"]?.GetValue<string>() ?? "";
        var segments = obj["Segment"]?.AsArray();
        var hasFlightSegments = segments != null && segments.Any(s =>
            s?["start_airport_code"] != null || s?["end_airport_code"] != null);
        var hasRailSegments = segments != null && segments.Any(s =>
            s?["start_station_name"] != null || s?["StartStationAddress"] != null);

        // --- Flights ---
        if (hasFlightSegments)
        {
            foreach (var seg in segments!)
            {
                if (seg == null) continue;
                await ImportFlightSegmentAsync(db, seg, obj, trip, profileMap, result);
            }
            return;
        }

        // --- Rail ---
        if (hasRailSegments || displayName.Equals("Rail", StringComparison.OrdinalIgnoreCase))
        {
            if (segments != null)
            {
                foreach (var seg in segments)
                {
                    if (seg == null) continue;
                    await ImportRailSegmentAsync(db, seg, obj, trip, profileMap, result);
                }
            }
            return;
        }

        // --- Lodging (has room_type, or Guest + Address, or supplier_name without segments) ---
        var hasLodgingFields = obj["room_type"] != null || obj["number_guests"] != null ||
                               (obj["Guest"] != null && obj["Address"] != null) ||
                               (obj["StartDateTime"] != null && obj["EndDateTime"] != null && obj["Address"] != null);
        if (hasLodgingFields)
        {
            await ImportLodgingAsync(db, obj, trip, profileMap, result);
            return;
        }

        // --- Activity / other (has DateTime + Address but no segments) ---
        if (obj["DateTime"] != null || (obj["StartDateTime"] != null && obj["Address"] != null))
        {
            await ImportActivityAsync(db, obj, trip, profileMap, result);
        }
    }

    private async Task ImportFlightSegmentAsync(
        ApplicationDbContext db, JsonNode seg, JsonNode parentObj,
        Trip trip, Dictionary<string, int> profileMap, ImportResult result)
    {
        var startDt = ParseDateTime(seg["StartDateTime"]);
        var endDt = ParseDateTime(seg["EndDateTime"]);
        if (startDt == null || endDt == null)
        {
            result.Warnings.Add($"Skipped flight segment: missing date/time.");
            return;
        }

        var origin = seg["start_airport_code"]?.GetValue<string>() ?? seg["start_city_name"]?.GetValue<string>();
        var destination = seg["end_airport_code"]?.GetValue<string>() ?? seg["end_city_name"]?.GetValue<string>();
        var originCity = seg["start_city_name"]?.GetValue<string>();
        var destCity = seg["end_city_name"]?.GetValue<string>();
        var provider = seg["marketing_airline"]?.GetValue<string>()
                       ?? parentObj["booking_site_name"]?.GetValue<string>();
        var flightNumber = BuildFlightNumber(
            seg["marketing_airline_code"]?.GetValue<string>(),
            seg["marketing_flight_number"]?.GetValue<string>());
        var seat = seg["seats"]?.GetValue<string>();
        var notes = parentObj["notes"]?.GetValue<string>();
        var confirmationCode = ExtractConfirmationCode(notes);

        var travellers = GetTravellerIdsFromObject(parentObj, profileMap);

        var transport = new Transportation
        {
            TripId = trip.Id,
            Type = TransportationType.Flight,
            Origin = origin,
            OriginCity = originCity,
            Destination = destination,
            DestinationCity = destCity,
            Provider = provider,
            FlightNumber = flightNumber,
            AssignedSeats = seat,
            ConfirmationCode = confirmationCode,
            Notes = notes,
            DepartureTime = startDt.Value,
            ArrivalTime = endDt.Value,
            DepartureTimezone = seg["StartDateTime"]?["timezone"]?.GetValue<string>(),
            ArrivalTimezone = seg["EndDateTime"]?["timezone"]?.GetValue<string>(),
            Travellers = travellers
        };
        db.Transportations.Add(transport);
        await db.SaveChangesAsync();
    }

    private async Task ImportRailSegmentAsync(
        ApplicationDbContext db, JsonNode seg, JsonNode parentObj,
        Trip trip, Dictionary<string, int> profileMap, ImportResult result)
    {
        var startDt = ParseDateTime(seg["StartDateTime"]);
        var endDt = ParseDateTime(seg["EndDateTime"]);
        if (startDt == null || endDt == null)
        {
            result.Warnings.Add($"Skipped rail segment: missing date/time.");
            return;
        }

        var origin = seg["start_station_name"]?.GetValue<string>()
            ?? seg["StartStationAddress"]?["city"]?.GetValue<string>();
        var destination = seg["end_station_name"]?.GetValue<string>()
            ?? seg["EndStationAddress"]?["city"]?.GetValue<string>();
        var originCity = seg["StartStationAddress"]?["city"]?.GetValue<string>() ?? origin;
        var destCity = seg["EndStationAddress"]?["city"]?.GetValue<string>() ?? destination;
        var provider = seg["carrier_name"]?.GetValue<string>()
            ?? parentObj["booking_site_name"]?.GetValue<string>();
        var seat = !string.IsNullOrEmpty(seg["coach_number"]?.GetValue<string>())
            ? $"Coach {seg["coach_number"]!.GetValue<string>()} Seat {seg["seats"]?.GetValue<string>()}".Trim()
            : seg["seats"]?.GetValue<string>();
        var notes = parentObj["notes"]?.GetValue<string>();
        var confirmationCode = ExtractConfirmationCode(notes);

        var travellers = GetTravellerIdsFromObject(parentObj, profileMap);

        var transport = new Transportation
        {
            TripId = trip.Id,
            Type = TransportationType.Train,
            Origin = origin,
            OriginCity = originCity,
            Destination = destination,
            DestinationCity = destCity,
            Provider = provider,
            AssignedSeats = seat,
            ConfirmationCode = confirmationCode,
            Notes = notes,
            DepartureTime = startDt.Value,
            ArrivalTime = endDt.Value,
            DepartureTimezone = seg["StartDateTime"]?["timezone"]?.GetValue<string>(),
            ArrivalTimezone = seg["EndDateTime"]?["timezone"]?.GetValue<string>(),
            Travellers = travellers
        };
        db.Transportations.Add(transport);
        await db.SaveChangesAsync();
    }

    private async Task ImportLodgingAsync(
        ApplicationDbContext db, JsonNode obj, Trip trip,
        Dictionary<string, int> profileMap, ImportResult result)
    {
        var startDt = ParseDateTime(obj["StartDateTime"]) ?? ParseDateTime(obj["EstimatedStartDateTime"]);
        var endDt = ParseDateTime(obj["EndDateTime"]);
        if (startDt == null || endDt == null)
        {
            result.Warnings.Add($"Skipped lodging '{obj["display_name"]}': missing date/time.");
            return;
        }

        var name = obj["supplier_name"]?.GetValue<string>()
            ?? obj["display_name"]?.GetValue<string>() ?? "Lodging";
        var address = obj["Address"]?["address"]?.GetValue<string>();
        var notes = obj["notes"]?.GetValue<string>();
        var timezone = obj["StartDateTime"]?["timezone"]?.GetValue<string>();
        var travellers = GetLodgingTravellersFromObject(obj, profileMap);

        var lodging = new Lodging
        {
            TripId = trip.Id,
            Type = LodgingType.Hotel,
            Name = name,
            Address = address,
            Notes = notes,
            StartDate = startDt.Value,
            EndDate = endDt.Value,
            Timezone = timezone,
            Travellers = travellers
        };
        db.Lodgings.Add(lodging);
        await db.SaveChangesAsync();
    }

    private async Task ImportActivityAsync(
        ApplicationDbContext db, JsonNode obj, Trip trip,
        Dictionary<string, int> profileMap, ImportResult result)
    {
        var startDt = ParseDateTime(obj["DateTime"]) ?? ParseDateTime(obj["StartDateTime"]);
        var endDt = ParseDateTime(obj["EndDateTime"]);

        if (startDt == null)
        {
            result.Warnings.Add($"Skipped activity '{obj["display_name"]}': missing date/time.");
            return;
        }

        var name = obj["supplier_name"]?.GetValue<string>()
            ?? obj["display_name"]?.GetValue<string>() ?? "Activity";
        var address = obj["Address"]?["address"]?.GetValue<string>();
        var notes = obj["notes"]?.GetValue<string>();
        var timezone = obj["DateTime"]?["timezone"]?.GetValue<string>()
            ?? obj["StartDateTime"]?["timezone"]?.GetValue<string>();
        var travellers = GetActivityTravellersFromObject(obj, profileMap);

        var activity = new Activity
        {
            TripId = trip.Id,
            Name = name,
            Address = address,
            Notes = notes,
            StartDate = startDt.Value,
            EndDate = endDt,
            Timezone = timezone,
            Travellers = travellers
        };
        db.Activities.Add(activity);
        await db.SaveChangesAsync();
    }

    // ── static helpers ───────────────────────────────────────────────────────

    private static void CollectTravellerNames(JsonNode obj, HashSet<string> names)
    {
        void AddName(JsonNode? node)
        {
            if (node == null) return;
            var first = node["first_name"]?.GetValue<string>() ?? "";
            var last = node["last_name"]?.GetValue<string>() ?? "";
            var full = $"{first} {last}".Trim();
            if (!string.IsNullOrEmpty(full)) names.Add(full);
        }

        var traveler = obj["Traveler"];
        if (traveler != null)
        {
            if (traveler is JsonArray arr)
                foreach (var t in arr) AddName(t);
            else
                AddName(traveler);
        }

        var guest = obj["Guest"];
        if (guest != null)
        {
            if (guest is JsonArray arr)
                foreach (var g in arr) AddName(g);
            else
                AddName(guest);
        }
    }

    private static List<TransportationTraveller> GetTravellerIdsFromObject(
        JsonNode obj, Dictionary<string, int> profileMap)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        CollectTravellerNames(obj, names);
        return names
            .Where(n => profileMap.ContainsKey(n))
            .Select(n => new TransportationTraveller { TravellerProfileId = profileMap[n] })
            .ToList();
    }

    private static List<LodgingTraveller> GetLodgingTravellersFromObject(
        JsonNode obj, Dictionary<string, int> profileMap)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        CollectTravellerNames(obj, names);
        return names
            .Where(n => profileMap.ContainsKey(n))
            .Select(n => new LodgingTraveller { TravellerProfileId = profileMap[n] })
            .ToList();
    }

    private static List<ActivityTraveller> GetActivityTravellersFromObject(
        JsonNode obj, Dictionary<string, int> profileMap)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        CollectTravellerNames(obj, names);
        return names
            .Where(n => profileMap.ContainsKey(n))
            .Select(n => new ActivityTraveller { TravellerProfileId = profileMap[n] })
            .ToList();
    }

    private static DateTime? ParseDateTime(JsonNode? dtNode)
    {
        if (dtNode == null) return null;
        var date = dtNode["date"]?.GetValue<string>();
        var time = dtNode["time"]?.GetValue<string>() ?? "00:00:00";
        if (date == null) return null;
        if (DateTime.TryParse($"{date}T{time}", out var result))
            return result;
        return null;
    }

    private static string? BuildFlightNumber(string? airlineCode, string? number)
    {
        if (string.IsNullOrWhiteSpace(airlineCode) && string.IsNullOrWhiteSpace(number)) return null;
        return $"{airlineCode}{number}".Trim();
    }

    private static string? ExtractConfirmationCode(string? notes)
    {
        if (string.IsNullOrEmpty(notes)) return null;
        // Common patterns: "Booking Reference XXXXXXX", "Record Locator XXXXXXX", "Ref: XXXXXXX"
        var patterns = new[] { "Booking Reference ", "Record Locator ", "Ref: ", "NRS Booking Reference " };
        foreach (var pattern in patterns)
        {
            var idx = notes.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) continue;
            var start = idx + pattern.Length;
            var end = notes.IndexOfAny([' ', ',', '\n', '\r'], start);
            return end < 0 ? notes[start..] : notes[start..end];
        }
        return null;
    }

    private static async Task<Dictionary<string, int>> ResolveTravellerProfilesAsync(
        ApplicationDbContext db,
        HashSet<string> travellerNames,
        string ownerId,
        string ownerLegalName,
        ImportResult result)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var name in travellerNames)
        {
            var existing = await db.TravellerProfiles
                .Where(p => p.OwnerId == ownerId && p.LegalName == name)
                .FirstOrDefaultAsync();

            if (existing != null)
            {
                map[name] = existing.Id;
                continue;
            }

            var profile = new TravellerProfile
            {
                LegalName = name,
                OwnerId = ownerId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            db.TravellerProfiles.Add(profile);
            await db.SaveChangesAsync();
            map[name] = profile.Id;

            if (!name.Equals(ownerLegalName, StringComparison.OrdinalIgnoreCase))
                result.Warnings.Add($"Created new traveller profile: {name}");
        }

        return map;
    }
}
