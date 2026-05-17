using Viagem.Data.Models;

namespace Viagem.Services;

public interface ITransportationService
{
    Task<List<Transportation>> GetTripTransportationsAsync(int tripId);
    Task<Transportation?> GetTransportationAsync(int id);
    Task<Transportation> CreateAsync(Transportation transportation);
    Task<Transportation> UpdateAsync(Transportation transportation);
    Task DeleteAsync(int id);
}
