using Viagem.Data.Models;

namespace Viagem.Data.Repositories.Interfaces;

public interface ITransportationRepository
{
    Task<List<Transportation>> GetByTripAsync(int tripId);
    Task<Transportation?> GetByIdAsync(int id);
    Task<Transportation> CreateAsync(Transportation transportation);
    Task<Transportation> UpdateAsync(Transportation transportation);
    Task UpdateTravellersAsync(int transportationId, IEnumerable<int> travellerProfileIds);
    Task DeleteAsync(int id);
}
