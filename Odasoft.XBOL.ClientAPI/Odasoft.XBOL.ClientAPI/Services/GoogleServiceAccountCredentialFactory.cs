using Google.Apis.Auth.OAuth2;

namespace Odasoft.XBOL.ClientAPI.Services;

public static class GoogleServiceAccountCredentialFactory
{
    public static GoogleCredential Create(string? serviceAccountJson, string? serviceAccountJsonPath)
    {
        return !string.IsNullOrWhiteSpace(serviceAccountJson)
            ? CredentialFactory
                .FromJson<ServiceAccountCredential>(serviceAccountJson)
                .ToGoogleCredential()
            : CredentialFactory
                .FromFile<ServiceAccountCredential>(serviceAccountJsonPath!)
                .ToGoogleCredential();
    }

    public static bool Validate(string? serviceAccountJson, string? serviceAccountJsonPath)
    {
        try
        {
            Create(serviceAccountJson, serviceAccountJsonPath);
            return true;
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or InvalidOperationException or UnauthorizedAccessException)
        {
            return false;
        }
    }
}
