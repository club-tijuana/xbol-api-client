namespace Odasoft.XBOL.ClientAPI.Services;

public interface IFirebaseTenantAuthClient
{
    Task UpdateUserAsync(FirebaseClientUserUpdate update, CancellationToken cancellationToken);

    Task DeleteUserAsync(string firebaseUid, CancellationToken cancellationToken);

    Task<string> CreateCustomTokenAsync(string firebaseUid, CancellationToken cancellationToken);
}
