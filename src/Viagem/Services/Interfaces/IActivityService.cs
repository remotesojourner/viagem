using Viagem.Services.ViewModels;

namespace Viagem.Services.Interfaces;

public interface IActivityService
{
    Task<List<ActivityViewModel>> GetTripActivitiesAsync(int tripId);
    Task<ActivityViewModel?> GetActivityAsync(int id);
    Task<ActivityViewModel> CreateAsync(CreateActivityRequest request);
    Task<ActivityViewModel?> UpdateAsync(UpdateActivityRequest request);
    Task DeleteAsync(int id);
}
