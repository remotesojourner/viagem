namespace Viagem.Services.ViewModels;

public record NotificationViewModel(
    int Id,
    string Subject,
    string? Message,
    string? Sender,
    bool Read,
    DateTime CreatedAt);
