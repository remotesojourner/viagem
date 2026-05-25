using System.IO.Compression;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Viagem.Data;
using Viagem.Data.Models;
using Viagem.Services.ImportExport;

namespace Viagem.Services;

public class TripExportService(IDbContextFactory<ApplicationDbContext> dbFactory, IWebHostEnvironment env)
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    /// <summary>Exports a single trip owned/accessible by <paramref name="userId"/> as a zip stream.</summary>
    public async Task<(Stream ZipStream, string FileName)> ExportTripAsync(int tripId, string userId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        var trip = await LoadFullTripAsync(db, tripId, userId)
            ?? throw new InvalidOperationException("Trip not found or access denied.");

        var dto = BuildDto(trip);

        var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            // Cover image
            if (trip.CoverImagePath != null)
            {
                var coverDisk = Path.Combine(env.WebRootPath, "uploads",
                    trip.CoverImagePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(coverDisk))
                {
                    var ext = Path.GetExtension(trip.CoverImagePath);
                    var zipPath = $"cover/cover{ext}";
                    dto.CoverImageZipPath = zipPath;
                    var entry = zip.CreateEntry(zipPath, CompressionLevel.Optimal);
                    await using var entryStream = entry.Open();
                    await using var fileStream = File.OpenRead(coverDisk);
                    await fileStream.CopyToAsync(entryStream);
                }
            }

            // Attachments
            foreach (var att in trip.Attachments)
            {
                var attDisk = Path.Combine(env.WebRootPath, "uploads",
                    att.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(attDisk)) continue;

                var zipPath = $"attachments/{att.Id}_{att.FileName}";
                dto.Attachments.Add(new AttachmentExportDto
                {
                    FileName = att.FileName,
                    ContentType = att.ContentType,
                    ZipPath = zipPath
                });
                var entry = zip.CreateEntry(zipPath, CompressionLevel.Optimal);
                await using var entryStream = entry.Open();
                await using var fileStream = File.OpenRead(attDisk);
                await fileStream.CopyToAsync(entryStream);
            }

            // trip.json (written last so CoverImageZipPath and attachments list are complete)
            var jsonEntry = zip.CreateEntry("trip.json", CompressionLevel.Optimal);
            await using var jsonStream = jsonEntry.Open();
            await JsonSerializer.SerializeAsync(jsonStream, dto, JsonOpts);
        }

        ms.Seek(0, SeekOrigin.Begin);
        var safeName = string.Concat(trip.Name.Split(Path.GetInvalidFileNameChars()));
        return (ms, $"{safeName}_{trip.StartDate:yyyyMMdd}.viagem.zip");
    }

    /// <summary>Exports ALL trips owned by <paramref name="userId"/> as a single zip, one subfolder per trip.</summary>
    public async Task<(Stream ZipStream, string FileName)> ExportAllTripsAsync(string userId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        var tripIds = await db.Trips
            .Where(t => t.OwnerId == userId)
            .Select(t => t.Id)
            .ToListAsync();

        var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var tripId in tripIds)
            {
                var trip = await LoadFullTripAsync(db, tripId, userId);
                if (trip == null) continue;

                var dto = BuildDto(trip);
                var safeFolder = $"{string.Concat(trip.Name.Split(Path.GetInvalidFileNameChars()))}_{trip.StartDate:yyyyMMdd}_{tripId}";

                if (trip.CoverImagePath != null)
                {
                    var coverDisk = Path.Combine(env.WebRootPath, "uploads",
                        trip.CoverImagePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                    if (File.Exists(coverDisk))
                    {
                        var ext = Path.GetExtension(trip.CoverImagePath);
                        var zipPath = $"{safeFolder}/cover/cover{ext}";
                        dto.CoverImageZipPath = $"cover/cover{ext}";
                        var entry = zip.CreateEntry(zipPath, CompressionLevel.Optimal);
                        await using var entryStream = entry.Open();
                        await using var fileStream = File.OpenRead(coverDisk);
                        await fileStream.CopyToAsync(entryStream);
                    }
                }

                foreach (var att in trip.Attachments)
                {
                    var attDisk = Path.Combine(env.WebRootPath, "uploads",
                        att.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                    if (!File.Exists(attDisk)) continue;

                    var zipPath = $"{safeFolder}/attachments/{att.Id}_{att.FileName}";
                    dto.Attachments.Add(new AttachmentExportDto
                    {
                        FileName = att.FileName,
                        ContentType = att.ContentType,
                        ZipPath = $"attachments/{att.Id}_{att.FileName}"
                    });
                    var entry = zip.CreateEntry(zipPath, CompressionLevel.Optimal);
                    await using var entryStream = entry.Open();
                    await using var fileStream = File.OpenRead(attDisk);
                    await fileStream.CopyToAsync(entryStream);
                }

                var jsonEntry = zip.CreateEntry($"{safeFolder}/trip.json", CompressionLevel.Optimal);
                await using var jsonStream = jsonEntry.Open();
                await JsonSerializer.SerializeAsync(jsonStream, dto, JsonOpts);
            }
        }

        ms.Seek(0, SeekOrigin.Begin);
        return (ms, $"viagem_export_{DateTime.UtcNow:yyyyMMdd_HHmm}.zip");
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static async Task<Trip?> LoadFullTripAsync(ApplicationDbContext db, int tripId, string userId)
    {
        return await db.Trips
            .Include(t => t.Destinations).ThenInclude(d => d.Place)
            .Include(t => t.Travellers).ThenInclude(tt => tt.TravellerProfile)
                .ThenInclude(p => p!.Aliases)
            .Include(t => t.Transportations).ThenInclude(tr => tr.Expense)
            .Include(t => t.Transportations).ThenInclude(tr => tr.Travellers).ThenInclude(tt => tt.TravellerProfile)
            .Include(t => t.Lodgings).ThenInclude(l => l.Expense)
            .Include(t => t.Lodgings).ThenInclude(l => l.Travellers).ThenInclude(lt => lt.TravellerProfile)
            .Include(t => t.Activities).ThenInclude(a => a.Expense)
            .Include(t => t.Activities).ThenInclude(a => a.Travellers).ThenInclude(at => at.TravellerProfile)
            .Include(t => t.Expenses).ThenInclude(e => e.Splits).ThenInclude(s => s.TravellerProfile)
            .Include(t => t.Attachments)
            .Where(t => t.OwnerId == userId ||
                t.Travellers.Any(tt => tt.TravellerProfile != null && tt.TravellerProfile.LinkedUserId == userId))
            .FirstOrDefaultAsync(t => t.Id == tripId);
    }

    private static TripExportDto BuildDto(Trip trip)
    {
        var dto = new TripExportDto
        {
            Name = trip.Name,
            Description = trip.Description,
            Notes = trip.Notes,
            StartDate = trip.StartDate,
            EndDate = trip.EndDate,
            BudgetAmount = trip.BudgetAmount,
            BudgetCurrency = trip.BudgetCurrency
        };

        foreach (var dest in trip.Destinations)
            dto.Destinations.Add(new TripDestinationExportDto { PlaceId = dest.PlaceId, CustomName = dest.CustomName });

        foreach (var tt in trip.Travellers)
        {
            if (tt.TravellerProfile == null) continue;
            dto.Travellers.Add(new TravellerProfileExportDto
            {
                LegalName = tt.TravellerProfile.LegalName,
                Email = tt.TravellerProfile.Email,
                IsOrganiser = tt.IsOrganiser,
                CanEdit = tt.CanEdit,
                Aliases = tt.TravellerProfile.Aliases.Select(a => a.Alias).ToList()
            });
        }

        foreach (var tr in trip.Transportations)
            dto.Transportations.Add(new TransportationExportDto
            {
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
                CostAmount = tr.CostAmount,
                CostCurrency = tr.CostCurrency,
                TravellerLegalNames = tr.Travellers
                    .Where(t => t.TravellerProfile != null)
                    .Select(t => t.TravellerProfile!.LegalName)
                    .ToList()
            });

        foreach (var l in trip.Lodgings)
            dto.Lodgings.Add(new LodgingExportDto
            {
                Type = l.Type,
                Name = l.Name,
                Address = l.Address,
                ConfirmationCode = l.ConfirmationCode,
                Notes = l.Notes,
                Link = l.Link,
                StartDate = l.StartDate,
                EndDate = l.EndDate,
                Timezone = l.Timezone,
                CostAmount = l.CostAmount,
                CostCurrency = l.CostCurrency,
                TravellerLegalNames = l.Travellers
                    .Where(t => t.TravellerProfile != null)
                    .Select(t => t.TravellerProfile!.LegalName)
                    .ToList()
            });

        foreach (var a in trip.Activities)
            dto.Activities.Add(new ActivityExportDto
            {
                Name = a.Name,
                Description = a.Description,
                Address = a.Address,
                Notes = a.Notes,
                Link = a.Link,
                StartDate = a.StartDate,
                EndDate = a.EndDate,
                Timezone = a.Timezone,
                CostAmount = a.CostAmount,
                CostCurrency = a.CostCurrency,
                TravellerLegalNames = a.Travellers
                    .Where(t => t.TravellerProfile != null)
                    .Select(t => t.TravellerProfile!.LegalName)
                    .ToList()
            });

        // Only standalone expenses (linked ones are recreated when transport/lodging/activity is created)
        foreach (var e in trip.Expenses.Where(e => e.SourceType == null))
            dto.Expenses.Add(new ExpenseExportDto
            {
                Name = e.Name,
                Category = e.Category,
                Notes = e.Notes,
                Amount = e.Amount,
                Currency = e.Currency,
                OccurredOn = e.OccurredOn,
                Splits = e.Splits
                    .Where(s => s.TravellerProfile != null)
                    .ToDictionary(s => s.TravellerProfile!.LegalName, s => s.Amount)
            });

        return dto;
    }
}
