using Viagem.Data.Models;

namespace Viagem.Data.Repositories.Interfaces;

public interface ITravellerProfileRepository
{
    Task<List<TravellerProfile>> GetByUserAsync(string userId);
    Task<TravellerProfile?> GetByIdAsync(int id, string userId);
    Task<TravellerProfile?> GetByIdDirectAsync(int id);
    Task<TravellerProfile> CreateAsync(TravellerProfile profile);
    Task<TravellerProfile> UpdateAsync(TravellerProfile profile);
    Task DeleteAsync(int id, string userId);
    Task AddAliasAsync(int profileId, string alias);
    Task RemoveAliasAsync(int aliasId);
    Task EnsureExistsAsync(string userId, string email, string? name);
}
