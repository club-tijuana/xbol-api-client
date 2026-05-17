namespace Odasoft.XBOL.ClientAPI.Services;

public sealed record FirebaseClientUserUpdate(
    string FirebaseUid,
    string? DisplayName,
    string? PhoneNumber,
    bool Disabled);
