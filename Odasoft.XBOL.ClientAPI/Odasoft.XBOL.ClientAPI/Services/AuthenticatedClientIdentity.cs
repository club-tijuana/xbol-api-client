namespace Odasoft.XBOL.ClientAPI.Services;

public sealed record AuthenticatedClientIdentity(
    string FirebaseUid,
    string? Email,
    bool EmailVerified,
    string? Name,
    string? PhoneNumber,
    string? SignInProvider);
