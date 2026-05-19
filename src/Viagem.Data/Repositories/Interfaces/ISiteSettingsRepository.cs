namespace Viagem.Data.Repositories.Interfaces;

public interface ISiteSettingsRepository
{
    Task<T?> GetAsync<T>(string key) where T : class;
    Task SetAsync<T>(string key, T value) where T : class;
}
