using System.IO.Compression;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Viagem.Data;
using Viagem.Data.Models;
using Viagem.Services.ImportExport;

namespace Viagem.Services;

public class ImportResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public int TripId { get; set; }
    public string TripName { get; set; } = "";
    public List<string> Warnings { get; set; } = [];
}

public class TripImportService(
    IDbContextFactory<ApplicationDbContext> dbFactory,
    IWebHostEnvironment env)
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Imports a single Viagem zip file (produced by <see cref="TripExportService"/>)
    /// and creates the trip under <paramref name="ownerId"/>.
    /// </summary>
    public async Task<ImportResult> ImportTripZipAsync(Stream zipStream, string ownerId)
    {
        try
        {
            using var zip = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen: true);

            var jsonEntry = zip.GetEntry("trip.json")
                ?? throw new InvalidOperationException("Invalid Viagem zip: trip.json not found.");

            TripExportDto dto;
            await using (var jsonStream = jsonEntry.Open())
                dto = await JsonSerializer.DeserializeAsync<TripExportDto>(jsonStream, JsonOpts)
                      ?? throw new InvalidOperationException("Failed to parse trip.json.");

            await using var db = await dbFactory.CreateDbContextAsync();

            var result = new ImportResult { TripName = dto.Name };

            // Resolve or create traveller profiles
            var profileMap = await ResolveTravellerProfilesAsync(db, dto.Travellers, ownerId, result);

            // Create trip
            var trip = new Trip
            {
                OwnerId = ownerId,
                Name = dto.Name,
                Description = dto.Description,
                Notes = dto.Notes,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                BudgetAmount = dto.BudgetAmount,
                BudgetCurrency = dto.BudgetCurrency,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            foreach (var dest in dto.Destinations)
            {
                if (dest.PlaceId.HasValue)
                {
                    var placeExists = await db.Places.AnyAsync(p => p.Id == dest.PlaceId.Value);
                    if (placeExists)
                        trip.Destinations.Add(new TripDestination { PlaceId = dest.PlaceId });
                    else
                    {
                        result.Warnings.Add($"Place id {dest.PlaceId} not found in database; skipped.");
                    }
                }
                else if (!string.IsNullOrWhiteSpace(dest.CustomName))
                    trip.Destinations.Add(new TripDestination { CustomName = dest.CustomName });
            }

            foreach (var tv in dto.Travellers)
            {
                if (!profileMap.TryGetValue(tv.LegalName, out var profileId)) continue;
                trip.Travellers.Add(new TripTraveller
                {
                    TravellerProfileId = profileId,
                    CanEdit = tv.CanEdit,
                    IsOrganiser = tv.IsOrganiser
                });
            }

            db.Trips.Add(trip);
            await db.SaveChangesAsync();

            // Transportations
            foreach (var tr in dto.Transportations)
            {
                var transport = new Transportation
                {
                    Id = Guid.NewGuid(),
                    Type = tr.Type,
                    Origin = tr.Origin,
                    OriginCity = tr.OriginCity,
                    Destination = tr.Destination,
                    DestinationCity = tr.DestinationCity,
                    Provider = tr.Provider,
                    ConfirmationCode = tr.ConfirmationCode,
                    FlightNumber = tr.FlightNumber,
                    AssignedSeats = tr.AssignedSeats,
                    Notes = tr.Notes,
                    Link = tr.Link,
                    DepartureTime = tr.DepartureTime,
                    ArrivalTime = tr.ArrivalTime,
                    DepartureTimezone = tr.DepartureTimezone,
                    ArrivalTimezone = tr.ArrivalTimezone,
                    RentalCompany = tr.RentalCompany,
                    PickupLocation = tr.PickupLocation,
                    DropOffLocation = tr.DropOffLocation,
                    SpotNumber = tr.SpotNumber,
                    ParkingAddress = tr.ParkingAddress,
                    TravellerProfileIds = tr.TravellerLegalNames
                        .Where(n => profileMap.ContainsKey(n))
                        .Select(n => profileMap[n])
                        .ToList()
                };

                if (tr.CostAmount is > 0 && !string.IsNullOrEmpty(tr.CostCurrency))
                {
                    var expId = Guid.NewGuid();
                    var expense = new Expense
                    {
                        Id = expId,
                        Name = $"{transport.Type} – {transport.Origin ?? ""} → {transport.Destination ?? ""}",
                        Category = ExpenseCategory.Transport,
                        Amount = tr.CostAmount.Value,
                        Currency = tr.CostCurrency,
                        OccurredOn = transport.DepartureTime,
                        CreatedById = ownerId,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    trip.Expenses.Add(expense);
                    transport.ExpenseId = expId;
                }
                trip.Transportations.Add(transport);
            }

            // Lodgings
            foreach (var l in dto.Lodgings)
            {
                var lodging = new Lodging
                {
                    Id = Guid.NewGuid(),
                    Type = l.Type,
                    Name = l.Name,
                    Address = l.Address,
                    ConfirmationCode = l.ConfirmationCode,
                    Notes = l.Notes,
                    Link = l.Link,
                    StartDate = l.StartDate,
                    EndDate = l.EndDate,
                    Timezone = l.Timezone,
                    TravellerProfileIds = l.TravellerLegalNames
                        .Where(n => profileMap.ContainsKey(n))
                        .Select(n => profileMap[n])
                        .ToList()
                };

                if (l.CostAmount is > 0 && !string.IsNullOrEmpty(l.CostCurrency))
                {
                    var expId = Guid.NewGuid();
                    var expense = new Expense
                    {
                        Id = expId,
                        Name = $"{l.Type} – {l.Name}",
                        Category = ExpenseCategory.Accommodation,
                        Amount = l.CostAmount.Value,
                        Currency = l.CostCurrency,
                        OccurredOn = l.StartDate,
                        CreatedById = ownerId,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    trip.Expenses.Add(expense);
                    lodging.ExpenseId = expId;
                }
                trip.Lodgings.Add(lodging);
            }

            // Activities
            foreach (var a in dto.Activities)
            {
                var activity = new Activity
                {
                    Id = Guid.NewGuid(),
                    Name = a.Name,
                    Description = a.Description,
                    Address = a.Address,
                    Notes = a.Notes,
                    Link = a.Link,
                    StartDate = a.StartDate,
                    EndDate = a.EndDate,
                    Timezone = a.Timezone,
                    TravellerProfileIds = a.TravellerLegalNames
                        .Where(n => profileMap.ContainsKey(n))
                        .Select(n => profileMap[n])
                        .ToList()
                };

                if (a.CostAmount is > 0 && !string.IsNullOrEmpty(a.CostCurrency))
                {
                    var expId = Guid.NewGuid();
                    var expense = new Expense
                    {
                        Id = expId,
                        Name = a.Name,
                        Category = ExpenseCategory.Activities,
                        Amount = a.CostAmount.Value,
                        Currency = a.CostCurrency,
                        OccurredOn = a.StartDate,
                        CreatedById = ownerId,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    trip.Expenses.Add(expense);
                    activity.ExpenseId = expId;
                }
                trip.Activities.Add(activity);
            }

            // Standalone expenses
            foreach (var e in dto.Expenses)
            {
                var expense = new Expense
                {
                    Id = Guid.NewGuid(),
                    Name = e.Name,
                    Category = e.Category,
                    Notes = e.Notes,
                    Amount = e.Amount,
                    Currency = e.Currency,
                    OccurredOn = e.OccurredOn,
                    CreatedById = ownerId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                foreach (var (name, amount) in e.Splits)
                {
                    if (profileMap.TryGetValue(name, out var pid))
                        expense.Splits.Add(new ExpenseSplit { TravellerProfileId = pid, Amount = amount });
                }
                trip.Expenses.Add(expense);
            }
            db.Trips.Update(trip);
            await db.SaveChangesAsync();

            // Cover image
            if (!string.IsNullOrEmpty(dto.CoverImageZipPath))
            {
                var coverEntry = zip.GetEntry(dto.CoverImageZipPath);
                if (coverEntry != null)
                {
                    var ext = Path.GetExtension(dto.CoverImageZipPath);
                    var relPath = $"trips/{trip.Id}/cover{ext}";
                    var fullDir = Path.Combine(env.WebRootPath, "uploads", "trips", trip.Id.ToString());
                    Directory.CreateDirectory(fullDir);
                    var fullPath = Path.Combine(fullDir, $"cover{ext}");
                    await using var entryStream = coverEntry.Open();
                    await using var fs = File.Create(fullPath);
                    await entryStream.CopyToAsync(fs);
                    trip.CoverImagePath = $"/trips/{trip.Id}/cover{ext}";
                    db.Trips.Update(trip);
                    await db.SaveChangesAsync();
                }
            }

            // Attachments
            foreach (var att in dto.Attachments)
            {
                var attEntry = zip.GetEntry(att.ZipPath);
                if (attEntry == null) continue;

                var attDir = Path.Combine(env.WebRootPath, "uploads", "trips", trip.Id.ToString(), "attachments");
                Directory.CreateDirectory(attDir);
                var safeFileName = Path.GetFileName(att.FileName);
                var fullPath = Path.Combine(attDir, safeFileName);
                await using var entryStream = attEntry.Open();
                await using var fs = File.Create(fullPath);
                await entryStream.CopyToAsync(fs);

                trip.Attachments.Add(new TripAttachment
                {
                    Id = Guid.NewGuid(),
                    FileName = att.FileName,
                    FilePath = $"/trips/{trip.Id}/attachments/{safeFileName}",
                    ContentType = att.ContentType,
                    FileSize = fs.Length,
                    UploadedById = ownerId,
                    UploadedAt = DateTime.UtcNow
                });
            }
            db.Trips.Update(trip);
            await db.SaveChangesAsync();

            result.Success = true;
            result.TripId = trip.Id;
            return result;
        }
        catch (Exception ex)
        {
            return new ImportResult { Success = false, Error = ex.Message };
        }
    }

    /// <summary>
    /// Bulk-imports from a "all trips" zip produced by <see cref="TripExportService.ExportAllTripsAsync"/>.
    /// Each top-level folder contains a trip.json.
    /// </summary>
    public async Task<List<ImportResult>> ImportAllTripsZipAsync(Stream zipStream, string ownerId)
    {
        var results = new List<ImportResult>();
        using var zip = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen: true);

        // Discover all trip.json entries (one per folder)
        var tripJsonEntries = zip.Entries
            .Where(e => e.Name == "trip.json" && e.FullName.Contains('/'))
            .ToList();

        foreach (var jsonEntry in tripJsonEntries)
        {
            var folder = jsonEntry.FullName[..jsonEntry.FullName.LastIndexOf('/')];

            // Collect the files for this trip into a temporary sub-zip in memory
            var subZipMs = new MemoryStream();
            using (var subZip = new ZipArchive(subZipMs, ZipArchiveMode.Create, leaveOpen: true))
            {
                foreach (var entry in zip.Entries.Where(e => e.FullName.StartsWith(folder + "/")))
                {
                    var relativePath = entry.FullName[(folder.Length + 1)..];
                    if (string.IsNullOrEmpty(relativePath)) continue;
                    var subEntry = subZip.CreateEntry(relativePath, CompressionLevel.Fastest);
                    await using var src = entry.Open();
                    await using var dst = subEntry.Open();
                    await src.CopyToAsync(dst);
                }
            }

            subZipMs.Seek(0, SeekOrigin.Begin);
            var result = await ImportTripZipAsync(subZipMs, ownerId);
            results.Add(result);
        }

        return results;
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Matches travellers by legal name (case-insensitive) owned by <paramref name="ownerId"/>,
    /// creating new profiles for any that don't exist.
    /// Returns a map of legalName → profileId.
    /// </summary>
    private static async Task<Dictionary<string, int>> ResolveTravellerProfilesAsync(
        ApplicationDbContext db,
        List<TravellerProfileExportDto> travellers,
        string ownerId,
        ImportResult result)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var tv in travellers)
        {
            var existing = await db.TravellerProfiles
                .Where(p => p.OwnerId == ownerId && p.LegalName == tv.LegalName)
                .FirstOrDefaultAsync();

            if (existing != null)
            {
                map[tv.LegalName] = existing.Id;
                continue;
            }

            // Try matching by email
            if (!string.IsNullOrEmpty(tv.Email))
            {
                existing = await db.TravellerProfiles
                    .Where(p => p.OwnerId == ownerId && p.Email == tv.Email)
                    .FirstOrDefaultAsync();
                if (existing != null)
                {
                    map[tv.LegalName] = existing.Id;
                    continue;
                }
            }

            // Create new profile
            var profile = new TravellerProfile
            {
                LegalName = tv.LegalName,
                Email = tv.Email,
                OwnerId = ownerId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            foreach (var alias in tv.Aliases)
                profile.Aliases.Add(new TravellerProfileAlias { Alias = alias });

            db.TravellerProfiles.Add(profile);
            await db.SaveChangesAsync();
            map[tv.LegalName] = profile.Id;
            result.Warnings.Add($"Created new traveller profile: {tv.LegalName}");
        }

        return map;
    }
}
