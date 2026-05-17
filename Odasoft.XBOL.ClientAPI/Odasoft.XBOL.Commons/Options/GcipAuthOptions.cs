using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Odasoft.XBOL.Commons.Options;

public sealed class GcipAuthOptions
{
    public const string SectionName = "GcipAuth";

    [Required]
    [MinLength(1)]
    [Description("Firebase Auth tenant ID for client users.")]
    public string TenantId { get; init; } = string.Empty;

    [Required]
    [MinLength(1)]
    [Description("Firebase project ID.")]
    public string ProjectId { get; init; } = string.Empty;

    [Required]
    [MinLength(1)]
    [Description("Firebase Web API key used for client-tenant email/password auth API calls.")]
    public string ApiKey { get; init; } = string.Empty;

    [Description("Path to a Firebase service account JSON file.")]
    public string? ServiceAccountJsonPath { get; init; }

    [Description("Firebase service account JSON content.")]
    public string? ServiceAccountJson { get; init; }

    [Description("Enable linking client identities by Firebase phone_number claims. Leave disabled until phone login is released.")]
    public bool EnablePhoneIdentityLinking { get; init; }
}
