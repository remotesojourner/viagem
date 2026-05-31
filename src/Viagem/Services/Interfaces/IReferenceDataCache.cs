using Viagem.Services.ViewModels;

namespace Viagem.Services.Interfaces;

public interface IReferenceDataCache
{
    IReadOnlyList<PlaceViewModel> Places { get; }
    IReadOnlyList<AirportViewModel> Airports { get; }
    IReadOnlyList<AirlineViewModel> Airlines { get; }

    Task InitializeAsync();
    Task RefreshAsync();
}
