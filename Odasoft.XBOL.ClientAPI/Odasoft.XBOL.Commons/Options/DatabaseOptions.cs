using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Odasoft.XBOL.Commons.Options;

public sealed class DatabaseOptions
{
    public const string SectionName = "ConnectionStrings";

    [Required]
    [MinLength(1)]
    [Description("PostgreSQL connection string for the client database.")]
    public string Database { get; init; } = string.Empty;
}
