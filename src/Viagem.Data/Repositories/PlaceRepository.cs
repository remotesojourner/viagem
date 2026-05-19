using Microsoft.EntityFrameworkCore;
using Viagem.Data.Models;
using Viagem.Data.Repositories.Interfaces;

namespace Viagem.Data.Repositories;

public class PlaceRepository(ApplicationDbContext db) : IPlaceRepository
{
    public async Task<List<Place>> SearchAsync(string query, int limit = 20)
    {
        if (string.IsNullOrWhiteSpace(query)) return [];
        var lower = query.ToLower();
        return await db.Places
            .Where(p => p.Name.ToLower().Contains(lower) || (p.CountryName != null && p.CountryName.ToLower().Contains(lower)))
            .OrderBy(p => p.Name)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<Place?> GetByIdAsync(int id)
        => await db.Places.FindAsync(id);
}
