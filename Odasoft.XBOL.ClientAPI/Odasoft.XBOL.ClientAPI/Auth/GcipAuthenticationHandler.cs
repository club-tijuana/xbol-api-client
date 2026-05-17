using System.Security.Claims;
using System.Text.Json;
using System.Text.Encodings.Web;
using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Odasoft.XBOL.ClientAPI.Auth;

public sealed class GcipAuthenticationHandler : AuthenticationHandler<GcipAuthenticationOptions>
{
    public const string SchemeName = "Bearer";

    public const string FirebaseUidClaimType = "firebase.uid";
    public const string TenantClaimType = "firebase.tenant";
    public const string EmailVerifiedClaimType = "firebase.email_verified";
    public const string SignInProviderClaimType = "firebase.sign_in_provider";

    private readonly IClientFirebaseTokenVerifier _tokenVerifier;

    public GcipAuthenticationHandler(
        IOptionsMonitor<GcipAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IClientFirebaseTokenVerifier tokenVerifier)
        : base(options, logger, encoder)
    {
        _tokenVerifier = tokenVerifier;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authorization = Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(authorization))
        {
            return AuthenticateResult.NoResult();
        }

        if (!authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticateResult.NoResult();
        }

        var token = authorization["Bearer ".Length..].Trim();
        if (string.IsNullOrWhiteSpace(token))
        {
            return AuthenticateResult.Fail("Authorization header with Bearer scheme requires a token value.");
        }

        VerifiedFirebaseClientToken decoded;
        try
        {
            decoded = await _tokenVerifier.VerifyIdTokenAsync(token, Context.RequestAborted);
        }
        catch (FirebaseAuthException ex) when (ex.AuthErrorCode == AuthErrorCode.TenantIdMismatch)
        {
            return AuthenticateResult.Fail($"Token tenant does not match expected tenant '{Options.TenantId}'.");
        }
        catch (FirebaseAuthException ex)
        {
            return AuthenticateResult.Fail($"Firebase ID token verification failed: {ex.Message}");
        }

        var claims = BuildClaims(decoded, token);
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }

    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status401Unauthorized,
            Title = "Unauthorized",
            Detail = "A valid Firebase client-tenant ID token is required.",
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.2"
        };

        await Response.WriteAsJsonAsync(problem);
    }

    private static List<Claim> BuildClaims(VerifiedFirebaseClientToken token, string rawToken)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, token.Uid),
            new(FirebaseUidClaimType, token.Uid)
        };

        if (token.TenantId is not null)
        {
            claims.Add(new Claim(TenantClaimType, token.TenantId));
        }

        if (token.Claims.TryGetValue("email", out var email) && email is string emailString)
        {
            claims.Add(new Claim(ClaimTypes.Email, emailString));
        }

        if (token.Claims.TryGetValue("email_verified", out var emailVerified) && emailVerified is bool emailVerifiedBool)
        {
            claims.Add(new Claim(EmailVerifiedClaimType, emailVerifiedBool.ToString().ToLowerInvariant()));
        }

        if (token.Claims.TryGetValue("name", out var name) && name is string nameString)
        {
            claims.Add(new Claim(ClaimTypes.Name, nameString));
        }

        if (token.Claims.TryGetValue("phone_number", out var phoneNumber) && phoneNumber is string phoneNumberString)
        {
            claims.Add(new Claim(ClaimTypes.MobilePhone, phoneNumberString));
        }

        if ((token.Claims.TryGetValue("firebase", out var firebase)
            && TryReadSignInProvider(firebase, out var signInProviderString))
            || TryReadSignInProvider(rawToken, out signInProviderString))
        {
            claims.Add(new Claim(SignInProviderClaimType, signInProviderString));
        }

        return claims;
    }

    private static bool TryReadSignInProvider(object firebaseClaim, out string signInProvider)
    {
        signInProvider = string.Empty;

        if (firebaseClaim is IDictionary<string, object> firebaseClaims
            && firebaseClaims.TryGetValue("sign_in_provider", out var value)
            && value is string stringValue)
        {
            signInProvider = stringValue;
            return true;
        }

        if (firebaseClaim is JsonElement { ValueKind: JsonValueKind.Object } json
            && json.TryGetProperty("sign_in_provider", out var signInProviderProperty)
            && signInProviderProperty.ValueKind == JsonValueKind.String)
        {
            signInProvider = signInProviderProperty.GetString() ?? string.Empty;
            return !string.IsNullOrWhiteSpace(signInProvider);
        }

        return false;
    }

    private static bool TryReadSignInProvider(string rawToken, out string signInProvider)
    {
        signInProvider = string.Empty;
        var parts = rawToken.Split('.');
        if (parts.Length < 2)
        {
            return false;
        }

        try
        {
            var payload = Base64UrlDecode(parts[1]);
            using var json = JsonDocument.Parse(payload);
            if (json.RootElement.TryGetProperty("firebase", out var firebase)
                && firebase.TryGetProperty("sign_in_provider", out var provider)
                && provider.ValueKind == JsonValueKind.String)
            {
                signInProvider = provider.GetString() ?? string.Empty;
                return !string.IsNullOrWhiteSpace(signInProvider);
            }
        }
        catch (JsonException)
        {
            return false;
        }
        catch (FormatException)
        {
            return false;
        }

        return false;
    }

    private static byte[] Base64UrlDecode(string value)
    {
        var base64 = value.Replace('-', '+').Replace('_', '/');
        var padding = base64.Length % 4;
        if (padding > 0)
        {
            base64 = base64.PadRight(base64.Length + 4 - padding, '=');
        }

        return Convert.FromBase64String(base64);
    }
}
