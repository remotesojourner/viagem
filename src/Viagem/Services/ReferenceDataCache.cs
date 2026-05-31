using Microsoft.EntityFrameworkCore;
using Viagem.Data;
using Viagem.Services.Interfaces;
using Viagem.Services.ViewModels;

namespace Viagem.Services;

public class ReferenceDataCache(IServiceProvider serviceProvider) : IReferenceDataCache
{
    private List<PlaceViewModel> _places = [];
    private List<AirportViewModel> _airports = [];
    private List<AirlineViewModel> _airlines = [];

    public IReadOnlyList<PlaceViewModel> Places => _places;
    public IReadOnlyList<AirportViewModel> Airports => _airports;
    public IReadOnlyList<AirlineViewModel> Airlines => _airlines;

    public async Task InitializeAsync()
    {
        await LoadDataAsync();
    }

    public async Task RefreshAsync()
    {
        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var places = await db.Places.AsNoTracking().OrderBy(p => p.Name).ToListAsync();
        var airports = await db.Airports.AsNoTracking().ToListAsync();
        var airlines = await db.Airlines.AsNoTracking().ToListAsync();

        _places = places.Select(p => new PlaceViewModel(p.Id, p.Name, p.StateName, p.CountryName, p.CountryCode, p.Timezone)).ToList();
        _airports = airports.Select(a => new AirportViewModel(a.Id, a.IataCode, a.Name, a.Municipality, a.IsoCountry, a.Latitude, a.Longitude)).ToList();
        _airlines = airlines.Select(a => new AirlineViewModel(a.Id, a.Code, a.Name, a.LogoUrl)).ToList();
    }
}
