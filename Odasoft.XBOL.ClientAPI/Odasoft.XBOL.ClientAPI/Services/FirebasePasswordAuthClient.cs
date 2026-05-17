using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Odasoft.XBOL.Commons.Options;

namespace Odasoft.XBOL.ClientAPI.Services;

public sealed class FirebasePasswordAuthClient(
    HttpClient httpClient,
    IOptions<GcipAuthOptions> options) : IFirebasePasswordAuthClient
{
    private readonly GcipAuthOptions authOptions = options.Value;

    public async Task<FirebasePasswordAuthResult> SignUpAsync(
        string email,
        string password,
        CancellationToken cancellationToken)
    {
        return await SendPasswordAuthAsync(
            "v1/accounts:signUp",
            email,
            password,
            cancellationToken);
    }

    private async Task<FirebasePasswordAuthResult> SendPasswordAuthAsync(
        string path,
        string email,
        string password,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsJsonAsync(
            $"{path}?key={Uri.EscapeDataString(authOptions.ApiKey)}",
            new
            {
                email,
                password,
                tenantId = authOptions.TenantId,
                returnSecureToken = true
            },
            cancellationToken);

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw BuildException(response.StatusCode, content);
        }

        var result = JsonSerializer.Deserialize<FirebasePasswordAuthResponse>(
            content,
            JsonSerializerOptions.Web);

        if (result is null
            || string.IsNullOrWhiteSpace(result.LocalId)
            || string.IsNullOrWhiteSpace(result.IdToken))
        {
            throw new ClientAuthException(
                "Firebase sign-up did not return a usable client identity token.",
                StatusCodes.Status502BadGateway,
                ClientAuthProblemCodes.InvalidRegistration);
        }

        return new FirebasePasswordAuthResult(
            result.LocalId,
            result.IdToken,
            result.Email,
            result.RefreshToken);
    }

    private static ClientAuthException BuildException(HttpStatusCode statusCode, string content)
    {
        var message = TryReadFirebaseError(content) ?? "Firebase sign-up failed.";
        var apiStatus = statusCode == HttpStatusCode.Conflict
            || message.Contains("EMAIL_EXISTS", StringComparison.OrdinalIgnoreCase)
            ? StatusCodes.Status409Conflict
            : StatusCodes.Status400BadRequest;

        var code = message.Contains("EMAIL_EXISTS", StringComparison.OrdinalIgnoreCase)
            ? ClientAuthProblemCodes.FirebaseEmailExists
            : ClientAuthProblemCodes.InvalidRegistration;

        return new ClientAuthException(message, apiStatus, code);
    }

    private static string? TryReadFirebaseError(string content)
    {
        try
        {
            var error = JsonSerializer.Deserialize<FirebasePasswordAuthErrorResponse>(
                content,
                JsonSerializerOptions.Web);

            return error?.Error?.Message;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private sealed record FirebasePasswordAuthResponse(
        string LocalId,
        string IdToken,
        string? Email,
        string? RefreshToken);

    private sealed record FirebasePasswordAuthErrorResponse(FirebasePasswordAuthError? Error);

    private sealed record FirebasePasswordAuthError(string? Message);
}
