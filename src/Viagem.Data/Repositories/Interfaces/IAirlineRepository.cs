using Viagem.Data.Models;

namespace Viagem.Data.Repositories.Interfaces;

public interface IAirlineRepository
{
    Task<Airline?> GetByCodeAsync(string code);
    Task<List<Airline>> SearchAsync(string query, int limit = 5);
}
