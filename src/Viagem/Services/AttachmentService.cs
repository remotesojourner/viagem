using Microsoft.AspNetCore.Components.Forms;
using Viagem.Data.Models;
using Viagem.Data.Repositories.Interfaces;
using Viagem.Services.Interfaces;
using Viagem.Services.ViewModels;

namespace Viagem.Services;

public class AttachmentService(IAttachmentRepository repo, IWebHostEnvironment env) : IAttachmentService
{
    private const long MaxFileSizeBytes = 50 * 1024 * 1024; // 50 MB

    public async Task<List<AttachmentViewModel>> GetTripAttachmentsAsync(int tripId)
    {
        var items = await repo.GetByTripAsync(tripId);
        return items.Select(ToViewModel).ToList();
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
            TripId = tripId,
            FileName = file.Name,
            FilePath = $"/uploads/trips/{tripId}/{uniqueName}",
            ContentType = file.ContentType,
            FileSize = file.Size,
            UploadedById = userId,
            UploadedAt = DateTime.UtcNow
        };

        var saved = await repo.AddAsync(attachment);
        return ToViewModel(saved);
    }

    public async Task DeleteAsync(int id)
    {
        var attachment = await repo.GetByIdAsync(id);
        if (attachment == null) return;

        var physicalPath = Path.Combine(
            env.WebRootPath,
            attachment.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

        if (File.Exists(physicalPath))
            File.Delete(physicalPath);

        await repo.DeleteAsync(id);
    }

    // ── Mapping ───────────────────────────────────────────────────────────────

    private static AttachmentViewModel ToViewModel(TripAttachment a)
        => new(a.Id, a.FileName, a.FilePath, a.ContentType, a.FileSize, a.UploadedAt);
}
