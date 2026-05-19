using Microsoft.EntityFrameworkCore;
using Viagem.Data.Models;
using Viagem.Data.Repositories.Interfaces;

namespace Viagem.Data.Repositories;

public class AttachmentRepository(ApplicationDbContext db) : IAttachmentRepository
{
    public async Task<List<TripAttachment>> GetByTripAsync(int tripId)
        => await db.TripAttachments
            .Where(a => a.TripId == tripId)
            .OrderByDescending(a => a.UploadedAt)
            .ToListAsync();

    public async Task<TripAttachment?> GetByIdAsync(int id)
        => await db.TripAttachments.FindAsync(id);

    public async Task<TripAttachment> AddAsync(TripAttachment attachment)
    {
        db.TripAttachments.Add(attachment);
        await db.SaveChangesAsync();
        return attachment;
    }

    public async Task DeleteAsync(int id)
    {
        var a = await db.TripAttachments.FindAsync(id);
        if (a != null)
        {
            db.TripAttachments.Remove(a);
            await db.SaveChangesAsync();
        }
    }
}
