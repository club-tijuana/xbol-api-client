namespace Odasoft.XBOL.ClientAPI.Services;

public sealed record FirebasePasswordAuthResult(
    string FirebaseUid,
    string IdToken,
    string? Email,
    string? RefreshToken);
