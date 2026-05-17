using Viagem.Data.Models;

namespace Viagem.Services.Interfaces;

public interface ITravellerProfileService
{
    Task<List<TravellerProfile>> GetMyProfilesAsync(string userId);
    Task<TravellerProfile?> GetProfileAsync(int id, string userId);
    Task<TravellerProfile> CreateAsync(TravellerProfile profile);
    Task<TravellerProfile> UpdateAsync(TravellerProfile profile);
    Task DeleteAsync(int id, string userId);
    Task AddAliasAsync(int profileId, string alias);
    Task RemoveAliasAsync(int aliasId);
    Task EnsureProfileExistsForUserAsync(string userId, string email, string? name);
}
