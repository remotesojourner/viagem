using System.ComponentModel.DataAnnotations;

namespace Viagem.Data.Models;

public class TravellerProfile
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string LegalName { get; set; } = "";

    [MaxLength(200)]
    public string? Email { get; set; }

    /// <summary>The user who manages this profile (may differ from the linked user, e.g. a parent managing a child's profile).</summary>
    public string OwnerId { get; set; } = "";
    public ApplicationUser? Owner { get; set; }

    /// <summary>The registered user account associated with this traveller profile, if any.</summary>
    public string? LinkedUserId { get; set; }
    public ApplicationUser? LinkedUser { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<TravellerProfileAlias> Aliases { get; set; } = [];
    public ICollection<TravellerAdditionalField> AdditionalFields { get; set; } = [];
    public ICollection<TravellerProfileManager> Managers { get; set; } = [];
    public ICollection<TravellerAttachment> Attachments { get; set; } = [];
}

public class TravellerProfileAlias
{
    public int Id { get; set; }
    public int TravellerProfileId { get; set; }
    public TravellerProfile? TravellerProfile { get; set; }

    [Required, MaxLength(200)]
    public string Alias { get; set; } = "";
}

public class TravellerAdditionalField
{
    public int Id { get; set; }
    public int TravellerProfileId { get; set; }
    public TravellerProfile? TravellerProfile { get; set; }

    [Required]
    public string Key { get; set; } = "";
    public string Label { get; set; } = "";
    public string? Value { get; set; }
}

public class TravellerProfileManager
{
    public int Id { get; set; }
    public int TravellerProfileId { get; set; }
    public TravellerProfile? TravellerProfile { get; set; }
    public string ManagerUserId { get; set; } = "";
    public ApplicationUser? ManagerUser { get; set; }
}

public class TravellerAttachment
{
    public int Id { get; set; }
    public int TravellerProfileId { get; set; }
    public TravellerProfile? TravellerProfile { get; set; }
    public int AttachmentId { get; set; }
    public TripAttachment? Attachment { get; set; }
}
