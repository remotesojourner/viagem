using Microsoft.EntityFrameworkCore;
using Viagem.Data.Models;
using Viagem.Data.Repositories.Interfaces;

namespace Viagem.Data.Repositories;

public class TravellerProfileRepository(ApplicationDbContext db) : ITravellerProfileRepository
{
    public async Task<List<TravellerProfile>> GetByUserAsync(string userId)
        => await db.TravellerProfiles
            .Include(tp => tp.Aliases)
            .Include(tp => tp.AdditionalFields)
            .Include(tp => tp.Managers).ThenInclude(m => m.ManagerUser)
            .Where(tp => tp.OwnerId == userId || tp.Managers.Any(m => m.ManagerUserId == userId))
            .OrderBy(tp => tp.LegalName)
            .ToListAsync();

    public async Task<TravellerProfile?> GetByIdAsync(int id, string userId)
        => await db.TravellerProfiles
            .Include(tp => tp.Aliases)
            .Include(tp => tp.AdditionalFields)
            .Include(tp => tp.Managers).ThenInclude(m => m.ManagerUser)
            .Include(tp => tp.Attachments).ThenInclude(a => a.Attachment)
            .Where(tp => tp.OwnerId == userId || tp.Managers.Any(m => m.ManagerUserId == userId))
            .FirstOrDefaultAsync(tp => tp.Id == id);

    public async Task<TravellerProfile?> GetByIdDirectAsync(int id)
        => await db.TravellerProfiles
            .Include(tp => tp.Aliases)
            .Include(tp => tp.AdditionalFields)
            .FirstOrDefaultAsync(tp => tp.Id == id);

    public async Task<TravellerProfile> CreateAsync(TravellerProfile profile)
    {
        profile.CreatedAt = DateTime.UtcNow;
        profile.UpdatedAt = DateTime.UtcNow;
        db.TravellerProfiles.Add(profile);
        await db.SaveChangesAsync();
        return profile;
    }

    public async Task<TravellerProfile> UpdateAsync(TravellerProfile profile)
    {
        profile.UpdatedAt = DateTime.UtcNow;
        db.TravellerProfiles.Update(profile);
        await db.SaveChangesAsync();
        return profile;
    }

    public async Task DeleteAsync(int id, string userId)
    {
        var profile = await db.TravellerProfiles
            .FirstOrDefaultAsync(tp => tp.Id == id && tp.OwnerId == userId);
        if (profile != null)
        {
            db.TravellerProfiles.Remove(profile);
            await db.SaveChangesAsync();
        }
    }

    public async Task AddAliasAsync(int profileId, string alias)
    {
        if (string.IsNullOrWhiteSpace(alias)) return;
        db.TravellerProfileAliases.Add(new TravellerProfileAlias
        {
            TravellerProfileId = profileId,
            Alias = alias.Trim()
        });
        await db.SaveChangesAsync();
    }

    public async Task RemoveAliasAsync(int aliasId)
    {
        var alias = await db.TravellerProfileAliases.FindAsync(aliasId);
        if (alias != null)
        {
            db.TravellerProfileAliases.Remove(alias);
            await db.SaveChangesAsync();
        }
    }

    public async Task EnsureExistsAsync(string userId, string email, string? name)
    {
        var existing = await db.TravellerProfiles
            .FirstOrDefaultAsync(tp => tp.LinkedUserId == userId);
        if (existing != null) return;

        var invited = await db.TravellerProfiles
            .FirstOrDefaultAsync(tp => tp.Email == email && tp.LinkedUserId == null);
        if (invited != null)
        {
            invited.LinkedUserId = userId;
            if (string.IsNullOrEmpty(invited.OwnerId))
                invited.OwnerId = userId;
            await db.SaveChangesAsync();
            return;
        }

        db.TravellerProfiles.Add(new TravellerProfile
        {
            LegalName = name ?? email,
            Email = email,
            OwnerId = userId,
            LinkedUserId = userId
        });
        await db.SaveChangesAsync();
    }

    public async Task MergeAsync(int targetId, IReadOnlyList<int> sourceIds, string userId)
    {
        // Verify ownership of target and all sources
        var allIds = sourceIds.Append(targetId).ToList();
        var profiles = await db.TravellerProfiles
            .Include(tp => tp.Aliases)
            .Where(tp => allIds.Contains(tp.Id) && (tp.OwnerId == userId || tp.Managers.Any(m => m.ManagerUserId == userId)))
            .ToListAsync();

        var target = profiles.FirstOrDefault(p => p.Id == targetId)
            ?? throw new InvalidOperationException("Target profile not found or not accessible.");

        var sources = profiles.Where(p => sourceIds.Contains(p.Id)).ToList();
        if (sources.Count == 0) return;

        await using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            // 1. Add source names and their aliases as aliases on the target
            var existingAliases = target.Aliases.Select(a => a.Alias).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var src in sources)
            {
                // Add the source's legal name as an alias
                if (!existingAliases.Contains(src.LegalName) &&
                    !string.Equals(src.LegalName, target.LegalName, StringComparison.OrdinalIgnoreCase))
                {
                    db.TravellerProfileAliases.Add(new TravellerProfileAlias
                    {
                        TravellerProfileId = targetId,
                        Alias = src.LegalName
                    });
                    existingAliases.Add(src.LegalName);
                }

                // Add each source alias
                foreach (var alias in src.Aliases)
                {
                    if (!existingAliases.Contains(alias.Alias) &&
                        !string.Equals(alias.Alias, target.LegalName, StringComparison.OrdinalIgnoreCase))
                    {
                        db.TravellerProfileAliases.Add(new TravellerProfileAlias
                        {
                            TravellerProfileId = targetId,
                            Alias = alias.Alias
                        });
                        existingAliases.Add(alias.Alias);
                    }
                }
            }

            // 2. Re-point join table rows, removing duplicates
            // TripTraveller
            var tripRows = await db.TripTravellers
                .Where(r => sourceIds.Contains(r.TravellerProfileId))
                .ToListAsync();
            var existingTripTargetIds = await db.TripTravellers
                .Where(r => r.TravellerProfileId == targetId)
                .Select(r => r.TripId)
                .ToHashSetAsync();
            foreach (var row in tripRows)
            {
                if (existingTripTargetIds.Contains(row.TripId))
                    db.TripTravellers.Remove(row);
                else
                {
                    row.TravellerProfileId = targetId;
                    existingTripTargetIds.Add(row.TripId);
                }
            }

            // TransportationTraveller
            var transportRows = await db.TransportationTravellers
                .Where(r => sourceIds.Contains(r.TravellerProfileId))
                .ToListAsync();
            var existingTransportTargetIds = await db.TransportationTravellers
                .Where(r => r.TravellerProfileId == targetId)
                .Select(r => r.TransportationId)
                .ToHashSetAsync();
            foreach (var row in transportRows)
            {
                if (existingTransportTargetIds.Contains(row.TransportationId))
                    db.TransportationTravellers.Remove(row);
                else
                {
                    row.TravellerProfileId = targetId;
                    existingTransportTargetIds.Add(row.TransportationId);
                }
            }

            // LodgingTraveller
            var lodgingRows = await db.LodgingTravellers
                .Where(r => sourceIds.Contains(r.TravellerProfileId))
                .ToListAsync();
            var existingLodgingTargetIds = await db.LodgingTravellers
                .Where(r => r.TravellerProfileId == targetId)
                .Select(r => r.LodgingId)
                .ToHashSetAsync();
            foreach (var row in lodgingRows)
            {
                if (existingLodgingTargetIds.Contains(row.LodgingId))
                    db.LodgingTravellers.Remove(row);
                else
                {
                    row.TravellerProfileId = targetId;
                    existingLodgingTargetIds.Add(row.LodgingId);
                }
            }

            // ActivityTraveller
            var activityRows = await db.ActivityTravellers
                .Where(r => sourceIds.Contains(r.TravellerProfileId))
                .ToListAsync();
            var existingActivityTargetIds = await db.ActivityTravellers
                .Where(r => r.TravellerProfileId == targetId)
                .Select(r => r.ActivityId)
                .ToHashSetAsync();
            foreach (var row in activityRows)
            {
                if (existingActivityTargetIds.Contains(row.ActivityId))
                    db.ActivityTravellers.Remove(row);
                else
                {
                    row.TravellerProfileId = targetId;
                    existingActivityTargetIds.Add(row.ActivityId);
                }
            }

            // ExpenseSplit
            var splitRows = await db.ExpenseSplits
                .Where(r => sourceIds.Contains(r.TravellerProfileId))
                .ToListAsync();
            var existingExpenseTargetIds = await db.ExpenseSplits
                .Where(r => r.TravellerProfileId == targetId)
                .Select(r => r.ExpenseId)
                .ToHashSetAsync();
            foreach (var row in splitRows)
            {
                if (existingExpenseTargetIds.Contains(row.ExpenseId))
                    db.ExpenseSplits.Remove(row);
                else
                {
                    row.TravellerProfileId = targetId;
                    existingExpenseTargetIds.Add(row.ExpenseId);
                }
            }

            await db.SaveChangesAsync();

            // 3. Delete source profiles
            db.TravellerProfiles.RemoveRange(sources);
            await db.SaveChangesAsync();

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
