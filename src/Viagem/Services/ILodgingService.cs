using Viagem.Data.Models;

namespace Viagem.Services;

public interface ILodgingService
{
    Task<List<Lodging>> GetTripLodgingsAsync(int tripId);
    Task<Lodging?> GetLodgingAsync(int id);
    Task<Lodging> CreateAsync(Lodging lodging);
    Task<Lodging> UpdateAsync(Lodging lodging);
    Task DeleteAsync(int id);
}
