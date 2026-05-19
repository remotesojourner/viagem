using Microsoft.EntityFrameworkCore;
using Viagem.Data.Models;
using Viagem.Data.Repositories.Interfaces;

namespace Viagem.Data.Repositories;

public class AirlineRepository(ApplicationDbContext db) : IAirlineRepository
{
    public async Task<Airline?> GetByCodeAsync(string code)
        => await db.Airlines.FirstOrDefaultAsync(a => a.Code.ToUpper() == code.ToUpper().Trim());

    public async Task<List<Airline>> SearchAsync(string query, int limit = 5)
    {
        if (string.IsNullOrWhiteSpace(query)) return [];
        var q = query.ToUpper().Trim();
        var lower = query.ToLower().Trim();
        return await db.Airlines
            .Where(a => a.Code.ToUpper().StartsWith(q) || a.Name.ToLower().Contains(lower))
            .OrderByDescending(a => a.Code.ToUpper() == q)
            .ThenBy(a => a.Name)
            .Take(limit)
            .ToListAsync();
    }
}
