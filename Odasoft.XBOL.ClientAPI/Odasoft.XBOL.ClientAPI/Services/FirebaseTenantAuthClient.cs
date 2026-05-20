using FirebaseAdmin.Auth;
using FirebaseAdmin.Auth.Multitenancy;

namespace Odasoft.XBOL.ClientAPI.Services;

public sealed class FirebaseTenantAuthClient(TenantAwareFirebaseAuth tenantAuth) : IFirebaseTenantAuthClient
{
    public Task<UserRecord> CreateUserAsync(UserRecordArgs user, CancellationToken cancellationToken)
    {
        return tenantAuth.CreateUserAsync(user, cancellationToken);
    }

    public Task DeleteUserAsync(string firebaseUid, CancellationToken cancellationToken)
    {
        return tenantAuth.DeleteUserAsync(firebaseUid, cancellationToken);
    }

    public Task<string> CreateCustomTokenAsync(string firebaseUid, CancellationToken cancellationToken)
    {
        return tenantAuth.CreateCustomTokenAsync(firebaseUid, cancellationToken);
    }
}
