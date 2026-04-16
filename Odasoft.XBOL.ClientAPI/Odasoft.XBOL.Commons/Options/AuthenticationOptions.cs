using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Odasoft.XBOL.Commons.Options;

public sealed class AuthenticationOptions
{
    public const string SectionName = "Authentication";

    [Required]
    [MinLength(1)]
    [Description("Accounts permitted to authenticate against the Client API.")]
    public AllowedUserOptions[] AllowedUsers { get; init; } = Array.Empty<AllowedUserOptions>();
}

public sealed class AllowedUserOptions
{
    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required]
    [MinLength(1)]
    public string Password { get; init; } = string.Empty;
}
