using Viagem.Data.Models;

namespace Viagem.Services;

public interface IActivityService
{
    Task<List<Activity>> GetTripActivitiesAsync(int tripId);
    Task<Activity?> GetActivityAsync(int id);
    Task<Activity> CreateAsync(Activity activity);
    Task<Activity> UpdateAsync(Activity activity);
    Task DeleteAsync(int id);
}
