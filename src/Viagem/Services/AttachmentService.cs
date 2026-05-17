using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using Viagem.Data;
using Viagem.Data.Models;

namespace Viagem.Services;

public class AttachmentService(ApplicationDbContext db, IWebHostEnvironment env) : IAttachmentService
{
    private const long MaxFileSizeBytes = 50 * 1024 * 1024; // 50 MB

    public async Task<List<TripAttachment>> GetTripAttachmentsAsync(int tripId)
        => await db.TripAttachments
            .Where(a => a.TripId == tripId)
            .OrderByDescending(a => a.UploadedAt)
            .ToListAsync();

    public async Task<TripAttachment> UploadAsync(int tripId, string userId, IBrowserFile file)
    {
        var uploadsDir = Path.Combine(env.WebRootPath, "uploads", "trips", tripId.ToString());
        Directory.CreateDirectory(uploadsDir);

        var safeFileName = Path.GetFileNameWithoutExtension(file.Name)
            .Replace(" ", "_")
            .Replace("..", "_");
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

        db.TripAttachments.Add(attachment);
        await db.SaveChangesAsync();
        return attachment;
    }

    public async Task DeleteAsync(int id)
    {
        var a = await db.TripAttachments.FindAsync(id);
        if (a == null) return;

        var physicalPath = Path.Combine(env.WebRootPath, a.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(physicalPath)) File.Delete(physicalPath);

        db.TripAttachments.Remove(a);
        await db.SaveChangesAsync();
    }
}
