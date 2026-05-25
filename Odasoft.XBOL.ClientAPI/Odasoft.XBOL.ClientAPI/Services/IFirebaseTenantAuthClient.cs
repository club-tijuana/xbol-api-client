using FirebaseAdmin.Auth;

namespace Odasoft.XBOL.ClientAPI.Services;

public interface IFirebaseTenantAuthClient
{
    Task<UserRecord> CreateUserAsync(UserRecordArgs user, CancellationToken cancellationToken);

    Task DeleteUserAsync(string firebaseUid, CancellationToken cancellationToken);

    Task<string> CreateCustomTokenAsync(string firebaseUid, CancellationToken cancellationToken);
}
