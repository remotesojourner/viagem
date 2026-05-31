using Microsoft.AspNetCore.Components.Forms;
using Viagem.Data.Models;
using Viagem.Data.Repositories.Interfaces;
using Viagem.Services.Interfaces;
using Viagem.Services.ViewModels;

namespace Viagem.Services;

public class AttachmentService(ITripRepository tripRepo, IWebHostEnvironment env) : IAttachmentService
{
    private const long MaxFileSizeBytes = 50 * 1024 * 1024; // 50 MB

    public async Task<List<AttachmentViewModel>> GetTripAttachmentsAsync(int tripId)
    {
        var trip = await tripRepo.GetByIdAsync(tripId, "");
        if (trip == null) return [];
        return trip.Attachments.Select(ToViewModel).ToList();
    }

    public async Task<AttachmentViewModel> UploadAsync(int tripId, string userId, IBrowserFile file)
    {
        var uploadsDir = Path.Combine(env.WebRootPath, "uploads", "trips", tripId.ToString());
        Directory.CreateDirectory(uploadsDir);

        var ext = Path.GetExtension(file.Name);
        var uniqueName = $"{Guid.NewGuid():N}{ext}";
        var filePath = Path.Combine(uploadsDir, uniqueName);

        await using var stream = file.OpenReadStream(MaxFileSizeBytes);
        await using var dest = File.Create(filePath);
        await stream.CopyToAsync(dest);

        var attachment = new TripAttachment
        {
            Id = Guid.NewGuid(),
            FileName = file.Name,
            FilePath = $"/uploads/trips/{tripId}/{uniqueName}",
            ContentType = file.ContentType,
            FileSize = file.Size,
            UploadedById = userId,
            UploadedAt = DateTime.UtcNow
        };

        var trip = await tripRepo.GetByIdAsync(tripId, userId);
        if (trip != null)
        {
            trip.Attachments.Add(attachment);
            await tripRepo.UpdateAsync(trip);
        }
        
        return ToViewModel(attachment);
    }

    public Task DeleteAsync(Guid id)
    {
        // Not used/fully implemented for new schema since ID is string/Guid now.
        // Actually this needs to take Guid id and string userId and int tripId
        throw new NotImplementedException();
    }

    // ── Mapping ───────────────────────────────────────────────────────────────

    private static AttachmentViewModel ToViewModel(TripAttachment a)
        => new(a.Id, a.FileName, a.FilePath, a.ContentType, a.FileSize, a.UploadedAt);
}
