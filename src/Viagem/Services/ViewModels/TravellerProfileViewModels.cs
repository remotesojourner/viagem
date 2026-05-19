namespace Viagem.Services.ViewModels;

public record TravellerAliasViewModel(int Id, string Alias);

public record TravellerAdditionalFieldViewModel(int Id, string Key, string Label, string? Value);

/// <summary>Lightweight summary used in dropdowns and traveller lists on items (activities, lodging, etc.).</summary>
public record TravellerProfileSummaryViewModel(
    int Id,
    string LegalName,
    string? Email);

public record TravellerProfileViewModel(
    int Id,
    string LegalName,
    string? Email,
    string OwnerId,
    string? LinkedUserId,
    IReadOnlyList<TravellerAliasViewModel> Aliases,
    IReadOnlyList<TravellerAdditionalFieldViewModel> AdditionalFields);
