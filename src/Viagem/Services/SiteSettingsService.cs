using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Viagem.Data;
using Viagem.Data.Models;

namespace Viagem.Services;

public class SiteSettingsService(ApplicationDbContext db)
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

    public async Task<bool> IsRegistrationEnabledAsync()
    {
        var val = await GetAsync<RegistrationConfig>(Keys.RegistrationConfig);
        return val?.Enabled ?? true;
    }

    public async Task<bool> IsSmtpConfiguredAsync()
    {
        var config = await GetAsync<SmtpConfig>(Keys.SmtpConfig);
        return config != null && !string.IsNullOrWhiteSpace(config.Host);
    }

    public async Task<OidcExtraConfig> GetOidcExtraConfigAsync()
    {
        return await GetAsync<OidcExtraConfig>(Keys.OidcExtraConfig) ?? new OidcExtraConfig(false, false);
    }

    public async Task<UsersConfig> GetUsersConfigAsync()
    {
        return await GetAsync<UsersConfig>(Keys.UsersConfig) ?? new UsersConfig(false);
    }

    public static class Keys
    {
        public const string RegistrationConfig = "registration_config";
        public const string SmtpConfig = "smtp_config";
        public const string OAuth2Config = "oauth2_config";
        public const string OidcExtraConfig = "oidc_extra_config";
        public const string UsersConfig = "users_config";
        public const string FlightInfoProvider = "flight_info_provider";
        public const string OpenAiEndpointConfig = "openai_endpoint_config";
        public const string EmailSyncConfig = "email_sync_config";
    }

    // Strongly-typed config records
    public record RegistrationConfig(bool Enabled);

    public record SmtpConfig(
        string Host,
        int Port,
        string? Username,
        string? Password,
        string? FromAddress,
        string? FromName,
        bool UseSsl);

    public record OAuth2Config(
        bool Enabled,
        string? ProviderName,
        string? ClientId,
        string? ClientSecret,
        string? Authority);

    public record FlightInfoProviderConfig(
        bool Enabled,
        string? Provider,
        string? ApiKey);

    public record OpenAiEndpointConfig(
        bool Enabled,
        string? Endpoint,
        string? ApiKey,
        string? Model);

    public record EmailSyncConfig(
        bool Enabled,
        string? ImapHost,
        int ImapPort,
        string? ImapUser,
        string? ImapPassword,
        string? FilterEmailAddress);

    public record OidcExtraConfig(
        bool DisablePasswordLogin,
        bool AutoRedirect);

    public record UsersConfig(bool GravatarEnabled);
}
