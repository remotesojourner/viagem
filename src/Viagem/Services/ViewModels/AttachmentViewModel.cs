namespace Viagem.Services.ViewModels;

public record AttachmentViewModel(
    int Id,
    string FileName,
    string FilePath,
    string? ContentType,
    long FileSize,
    DateTime UploadedAt);
