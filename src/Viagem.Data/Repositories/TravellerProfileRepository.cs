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
}
