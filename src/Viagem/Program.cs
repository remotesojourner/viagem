using LumexUI.Extensions;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Viagem.Data;
using Viagem.Components;
using Viagem.Components.Account;
using Viagem.Data.Repositories;
using Viagem.Data.Repositories.Interfaces;
using Viagem.Services;
using Viagem.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString,
        o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)
              .MigrationsAssembly("Viagem.Data")));

builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString,
        o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)
              .MigrationsAssembly("Viagem.Data")), ServiceLifetime.Scoped);

builder.Services.AddIdentityCore<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

// LumexUI services
builder.Services.AddLumexServices();

// Repositories
builder.Services.AddScoped<ITripRepository, TripRepository>();
builder.Services.AddScoped<IPlaceRepository, PlaceRepository>();
builder.Services.AddScoped<IAirportRepository, AirportRepository>();
builder.Services.AddScoped<IAirlineRepository, AirlineRepository>();
builder.Services.AddScoped<ITravellerProfileRepository, TravellerProfileRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<ISiteSettingsRepository, SiteSettingsRepository>();

// Domain services
builder.Services.AddSingleton<IReferenceDataCache, ReferenceDataCache>();
builder.Services.AddScoped<ITripService, TripService>();
builder.Services.AddScoped<IPlaceService, PlaceService>();
builder.Services.AddScoped<IAirportService, AirportService>();
builder.Services.AddScoped<IAirlineService, AirlineService>();
builder.Services.AddScoped<IFlightRouteService, FlightRouteService>();
builder.Services.AddScoped<TripStateContainer>();
builder.Services.AddScoped<ITravellerProfileService, TravellerProfileService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IAttachmentService, AttachmentService>();
builder.Services.AddScoped<DataSeedService>();

// Import / Export
builder.Services.AddScoped<TripExportService>();
builder.Services.AddScoped<TripImportService>();
builder.Services.AddScoped<TripItImportService>();

// Travel stats
builder.Services.AddScoped<TravelStatsCalculator>();
builder.Services.AddHostedService<TravelStatsBackgroundService>();
builder.Services.AddScoped<SiteSettingsService>();
builder.Services.AddHttpClient("seed", c => c.Timeout = TimeSpan.FromMinutes(5));
builder.Services.AddHttpClient("adsbdb", c => c.Timeout = TimeSpan.FromSeconds(10));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseAntiforgery();

app.MapStaticAssets();
var uploadsDir = Path.Combine(builder.Environment.WebRootPath, "uploads");
if (!Directory.Exists(uploadsDir))
{
    Directory.CreateDirectory(uploadsDir);
}
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsDir),
    RequestPath = "/uploads"
});
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapAdditionalIdentityEndpoints();

// Export endpoints
app.MapGet("/api/trips/{id:int}/export", async (int id, HttpContext ctx,
    TripExportService exportSvc, ApplicationDbContext db) =>
{
    var userId = ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (userId == null) return Results.Unauthorized();
    try
    {
        var (stream, fileName) = await exportSvc.ExportTripAsync(id, userId);
        return Results.File(stream, "application/zip", fileName);
    }
    catch (InvalidOperationException ex)
    {
        return Results.NotFound(ex.Message);
    }
}).RequireAuthorization();

app.MapGet("/api/trips/export-all", async (HttpContext ctx, TripExportService exportSvc) =>
{
    var userId = ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (userId == null) return Results.Unauthorized();
    var (stream, fileName) = await exportSvc.ExportAllTripsAsync(userId);
    return Results.File(stream, "application/zip", fileName);
}).RequireAuthorization();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();
    
    var cache = scope.ServiceProvider.GetRequiredService<IReferenceDataCache>();
    await cache.InitializeAsync();

    var seeder = scope.ServiceProvider.GetRequiredService<DataSeedService>();
    await seeder.SeedAsync();

    if (!db.Users.Any(u => u.IsAdmin) && db.Users.Any())
    {
        var firstUser = db.Users.OrderBy(u => u.Id).First();
        firstUser.IsAdmin = true;
        db.SaveChanges();
    }
}

app.Run();
