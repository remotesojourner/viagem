using Viagem.Data.Models;
using Viagem.Data.Repositories.Interfaces;
using Viagem.Services.Interfaces;
using Viagem.Services.ViewModels;

namespace Viagem.Services;

public class TravellerProfileService(ITravellerProfileRepository repo) : ITravellerProfileService
{
    public async Task<List<TravellerProfileViewModel>> GetMyProfilesAsync(string userId)
    {
        var profiles = await repo.GetByUserAsync(userId);
        return profiles.Select(ToViewModel).ToList();
    }

    public async Task<TravellerProfileViewModel?> GetProfileAsync(int id, string userId)
    {
        var profile = await repo.GetByIdAsync(id, userId);
        return profile == null ? null : ToViewModel(profile);
    }

    public async Task<TravellerProfileViewModel> CreateAsync(CreateTravellerProfileRequest request)
    {
        var entity = new TravellerProfile
        {
            OwnerId = request.OwnerId,
            LegalName = request.LegalName,
            Email = request.Email
        };

        var created = await repo.CreateAsync(entity);
        var full = await repo.GetByIdAsync(created.Id, request.OwnerId);
        return ToViewModel(full!);
    }

    public async Task<TravellerProfileViewModel?> UpdateAsync(string userId, UpdateTravellerProfileRequest request)
    {
        var existing = await repo.GetByIdAsync(request.Id, userId);
        if (existing == null) return null;

        existing.LegalName = request.LegalName;
        existing.Email = request.Email;

        await repo.UpdateAsync(existing);
        var full = await repo.GetByIdAsync(request.Id, userId);
        return full == null ? null : ToViewModel(full);
    }

    public Task DeleteAsync(int id, string userId) => repo.DeleteAsync(id, userId);

    public Task AddAliasAsync(int profileId, string alias) => repo.AddAliasAsync(profileId, alias);

    public Task RemoveAliasAsync(int aliasId) => repo.RemoveAliasAsync(aliasId);

    public Task EnsureProfileExistsForUserAsync(string userId, string email, string? name)
        => repo.EnsureExistsAsync(userId, email, name);

    public async Task LinkUserAsync(int profileId, string linkedUserId, string? ownerId = null)
    {
        var existing = await repo.GetByIdDirectAsync(profileId);
        if (existing == null) return;
        existing.LinkedUserId = linkedUserId;
        if (!string.IsNullOrEmpty(ownerId))
            existing.OwnerId = ownerId;
        await repo.UpdateAsync(existing);
    }

    public Task MergeAsync(int targetId, IReadOnlyList<int> sourceIds, string userId)
        => repo.MergeAsync(targetId, sourceIds, userId);

    // ── Mapping ───────────────────────────────────────────────────────────────

    private static TravellerAliasViewModel ToAliasViewModel(TravellerProfileAlias a)
        => new(a.Id, a.Alias);

    private static TravellerAdditionalFieldViewModel ToFieldViewModel(TravellerAdditionalField f)
        => new(f.Id, f.Key, f.Label, f.Value);

    private static TravellerProfileViewModel ToViewModel(TravellerProfile p)
        => new(p.Id, p.LegalName, p.Email, p.OwnerId, p.LinkedUserId,
            p.Aliases.Select(ToAliasViewModel).ToList(),
            p.AdditionalFields.Select(ToFieldViewModel).ToList());
}
