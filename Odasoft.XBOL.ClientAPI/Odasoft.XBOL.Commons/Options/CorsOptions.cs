using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Odasoft.XBOL.Commons.Options;

public sealed class CorsOptions
{
    public const string SectionName = "Cors";

    [Required]
    [MinLength(1)]
    [Description("CORS policy name registered in the pipeline.")]
    public string PolicyName { get; init; } = string.Empty;

    [Required]
    [MinLength(1)]
    [Description("Origins allowed to call the API (exact match).")]
    public string[] AcceptedOrigins { get; init; } = Array.Empty<string>();
}
