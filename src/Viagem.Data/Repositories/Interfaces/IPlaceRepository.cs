using Viagem.Data.Models;

namespace Viagem.Data.Repositories.Interfaces;

public interface IPlaceRepository
{
    Task<List<Place>> SearchAsync(string query, int limit = 20);
    Task<Place?> GetByIdAsync(int id);
}
