using Microsoft.EntityFrameworkCore;
using Viagem.Data.Models;
using Viagem.Data.Repositories.Interfaces;

namespace Viagem.Data.Repositories;

public class AirportRepository(ApplicationDbContext db) : IAirportRepository
{
    public async Task<List<Airport>> SearchAsync(string query, int limit = 10)
    {
        if (string.IsNullOrWhiteSpace(query)) return [];
        var q = query.ToUpper().Trim();
        var lower = query.ToLower().Trim();
        return await db.Airports
            .Where(a => a.IataCode.ToUpper().StartsWith(q)
                     || a.Name.ToLower().Contains(lower)
                     || (a.Municipality != null && a.Municipality.ToLower().Contains(lower)))
            .OrderByDescending(a => a.IataCode.ToUpper() == q)
            .ThenByDescending(a => a.IataCode.ToUpper().StartsWith(q))
            .ThenBy(a => a.Name)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<Airport?> GetByCodeAsync(string iataCode)
        => await db.Airports.FirstOrDefaultAsync(a => a.IataCode.ToUpper() == iataCode.ToUpper().Trim());
}
