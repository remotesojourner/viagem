using Microsoft.AspNetCore.Identity;

namespace Viagem.Data;

public class ApplicationUser : IdentityUser
{
    public string? FullName { get; set; }
    public string? CurrencyCode { get; set; }
    public string? Timezone { get; set; }
    public string? MapsProvider { get; set; }
    public string? WebsiteAppearance { get; set; } = "system";
    public string? AvatarPath { get; set; }
    public bool IsAdmin { get; set; }
    public bool RequirePasswordChange { get; set; } = false;
}

