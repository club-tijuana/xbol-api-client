namespace Odasoft.XBOL.ClientAPI.Services;

public interface IFirebaseClientAuthClient
{
    Task<FirebaseClientUser> CreateUserAsync(
        CreateFirebaseClientUserRequest user,
        CancellationToken cancellationToken);

    Task DeleteUserAsync(string firebaseUid, CancellationToken cancellationToken);

    Task<string> CreateCustomTokenAsync(string firebaseUid, CancellationToken cancellationToken);
}

public sealed record CreateFirebaseClientUserRequest(
    string Email,
    string Password,
    string DisplayName,
    bool EmailVerified,
    bool Disabled);

public sealed record FirebaseClientUser(
    string Uid,
    string? Email,
    bool EmailVerified,
    string? DisplayName,
    string? PhoneNumber);
