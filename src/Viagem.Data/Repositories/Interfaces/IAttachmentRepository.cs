using Viagem.Data.Models;

namespace Viagem.Data.Repositories.Interfaces;

public interface IAttachmentRepository
{
    Task<List<TripAttachment>> GetByTripAsync(int tripId);
    Task<TripAttachment?> GetByIdAsync(int id);
    Task<TripAttachment> AddAsync(TripAttachment attachment);
    Task DeleteAsync(int id);
}
