using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Odasoft.XBOL.Commons.Options;

public sealed class GcipAuthOptions
{
    public const string SectionName = "GcipAuth";

    [Required]
    [MinLength(1)]
    [Description("Firebase project ID.")]
    public string ProjectId { get; init; } = string.Empty;

    [Description("Path to a Firebase service account JSON file.")]
    public string? ServiceAccountJsonPath { get; init; }

    [Description("Firebase service account JSON content.")]
    public string? ServiceAccountJson { get; init; }
}
