using Viagem.Data.Models;

namespace Viagem.Services;

public interface ITravellerProfileService
{
    Task<List<TravellerProfile>> GetMyProfilesAsync(string userId);
    Task<TravellerProfile?> GetProfileAsync(int id, string userId);
    Task<TravellerProfile> CreateAsync(TravellerProfile profile);
    Task<TravellerProfile> UpdateAsync(TravellerProfile profile);
    Task DeleteAsync(int id, string userId);
    Task AddAliasAsync(int profileId, string alias);
    Task RemoveAliasAsync(int aliasId);
    /// <summary>Finds an unlinked profile with the given email, links it to the user, or creates a new profile.</summary>
    Task EnsureProfileExistsForUserAsync(string userId, string email, string? name);
}
