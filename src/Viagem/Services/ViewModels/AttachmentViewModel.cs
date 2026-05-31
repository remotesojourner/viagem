namespace Viagem.Services.ViewModels;

public record AttachmentViewModel(
    Guid Id,
    string FileName,
    string FilePath,
    string? ContentType,
    long FileSize,
    DateTime UploadedAt);
