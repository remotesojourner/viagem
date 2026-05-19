using Viagem.Services.ViewModels;

namespace Viagem.Services.Interfaces;

public interface ITransportationService
{
    Task<List<TransportationViewModel>> GetTripTransportationsAsync(int tripId);
    Task<TransportationViewModel?> GetTransportationAsync(int id);
    Task<TransportationViewModel> CreateAsync(CreateTransportationRequest request);
    Task<TransportationViewModel?> UpdateAsync(UpdateTransportationRequest request);
    Task DeleteAsync(int id);
}
