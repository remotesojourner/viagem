using Viagem.Data.Models;

namespace Viagem.Data.Repositories.Interfaces;

public interface IActivityRepository
{
    Task<List<Activity>> GetByTripAsync(int tripId);
    Task<Activity?> GetByIdAsync(int id);
    Task<Activity> CreateAsync(Activity activity);
    Task<Activity> UpdateAsync(Activity activity);
    Task DeleteAsync(int id);
}
