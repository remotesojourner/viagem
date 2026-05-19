using Viagem.Data.Models;

namespace Viagem.Data.Repositories.Interfaces;

public interface IAirportRepository
{
    Task<List<Airport>> SearchAsync(string query, int limit = 10);
    Task<Airport?> GetByCodeAsync(string iataCode);
}
