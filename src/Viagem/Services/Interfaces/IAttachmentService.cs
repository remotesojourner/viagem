using Microsoft.AspNetCore.Components.Forms;
using Viagem.Services.ViewModels;

namespace Viagem.Services.Interfaces;

public interface IAttachmentService
{
    Task<List<AttachmentViewModel>> GetTripAttachmentsAsync(int tripId);
    Task<AttachmentViewModel> UploadAsync(int tripId, string userId, IBrowserFile file);
    Task DeleteAsync(Guid id);
}
