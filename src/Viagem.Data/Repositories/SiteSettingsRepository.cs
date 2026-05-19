using System.Text.Json;
using Viagem.Data.Models;
using Viagem.Data.Repositories.Interfaces;

namespace Viagem.Data.Repositories;

public class SiteSettingsRepository(ApplicationDbContext db) : ISiteSettingsRepository
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        var setting = await db.SiteSettings.FindAsync(key);
        if (setting is null) return null;
        return JsonSerializer.Deserialize<T>(setting.Value, JsonOpts);
    }

    public async Task SetAsync<T>(string key, T value) where T : class
    {
        var json = JsonSerializer.Serialize(value, JsonOpts);
        var setting = await db.SiteSettings.FindAsync(key);
        if (setting is null)
            db.SiteSettings.Add(new SiteSetting { Key = key, Value = json });
        else
            setting.Value = json;
        await db.SaveChangesAsync();
    }
}
