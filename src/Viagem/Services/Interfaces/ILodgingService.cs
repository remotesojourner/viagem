using Viagem.Services.ViewModels;

namespace Viagem.Services.Interfaces;

public interface ILodgingService
{
    Task<List<LodgingViewModel>> GetTripLodgingsAsync(int tripId);
    Task<LodgingViewModel?> GetLodgingAsync(int id);
    Task<LodgingViewModel> CreateAsync(CreateLodgingRequest request);
    Task<LodgingViewModel?> UpdateAsync(UpdateLodgingRequest request);
    Task DeleteAsync(int id);
}
