using Viagem.Data.Repositories.Interfaces;

namespace Viagem.Services;

public class SiteSettingsService(ISiteSettingsRepository repo)
{
    public Task<T?> GetAsync<T>(string key) where T : class
        => repo.GetAsync<T>(key);

    public Task SetAsync<T>(string key, T value) where T : class
        => repo.SetAsync(key, value);

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
