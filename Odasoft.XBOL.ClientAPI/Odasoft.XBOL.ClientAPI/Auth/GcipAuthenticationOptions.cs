using Microsoft.AspNetCore.Authentication;

namespace Odasoft.XBOL.ClientAPI.Auth;

public sealed class GcipAuthenticationOptions : AuthenticationSchemeOptions
{
    public string TenantId { get; set; } = string.Empty;
}
