using Viagem.Services.ViewModels;

namespace Viagem.Services;

public class TripStateContainer
{
    private TripDetailViewModel? _trip;

    public TripDetailViewModel? Trip
    {
        get => _trip;
        set
        {
            _trip = value;
            NotifyStateChanged();
        }
    }

    public event Action? OnStateChange;

    private void NotifyStateChanged() => OnStateChange?.Invoke();
}
