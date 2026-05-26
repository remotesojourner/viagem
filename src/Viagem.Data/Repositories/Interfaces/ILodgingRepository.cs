using Viagem.Data.Models;

namespace Viagem.Data.Repositories.Interfaces;

public interface ILodgingRepository
{
    Task<List<Lodging>> GetByTripAsync(int tripId);
    Task<Lodging?> GetByIdAsync(int id);
    Task<Lodging> CreateAsync(Lodging lodging);
    Task<Lodging> UpdateAsync(Lodging lodging);
    Task UpdateTravellersAsync(int lodgingId, IEnumerable<int> travellerProfileIds);
    Task DeleteAsync(int id);
}
