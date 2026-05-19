using Viagem.Services.ViewModels;

namespace Viagem.Services.Interfaces;

public interface ITravellerProfileService
{
    Task<List<TravellerProfileViewModel>> GetMyProfilesAsync(string userId);
    Task<TravellerProfileViewModel?> GetProfileAsync(int id, string userId);
    Task<TravellerProfileViewModel> CreateAsync(CreateTravellerProfileRequest request);
    Task<TravellerProfileViewModel?> UpdateAsync(string userId, UpdateTravellerProfileRequest request);
    Task DeleteAsync(int id, string userId);
    Task AddAliasAsync(int profileId, string alias);
    Task RemoveAliasAsync(int aliasId);
    Task EnsureProfileExistsForUserAsync(string userId, string email, string? name);
    Task LinkUserAsync(int profileId, string linkedUserId, string? ownerId = null);
}
