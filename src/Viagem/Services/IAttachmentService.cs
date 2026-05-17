using Microsoft.AspNetCore.Components.Forms;
using Viagem.Data.Models;

namespace Viagem.Services;

public interface IAttachmentService
{
    Task<List<TripAttachment>> GetTripAttachmentsAsync(int tripId);
    Task<TripAttachment> UploadAsync(int tripId, string userId, IBrowserFile file);
    Task DeleteAsync(int id);
}
