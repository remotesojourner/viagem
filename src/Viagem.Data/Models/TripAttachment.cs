using System.ComponentModel.DataAnnotations;

namespace Viagem.Data.Models;

public class TripAttachment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(500)]
    public string FileName { get; set; } = "";

    [Required]
    public string FilePath { get; set; } = "";

    public string? ContentType { get; set; }
    public long FileSize { get; set; }

    public string UploadedById { get; set; } = "";

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
