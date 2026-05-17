using FirebaseAdmin.Auth;
using FirebaseAdmin.Auth.Multitenancy;

namespace Odasoft.XBOL.ClientAPI.Services;

public sealed class FirebaseTenantAuthClient(TenantAwareFirebaseAuth tenantAuth) : IFirebaseTenantAuthClient
{
    public Task UpdateUserAsync(FirebaseClientUserUpdate update, CancellationToken cancellationToken)
    {
        return tenantAuth.UpdateUserAsync(new UserRecordArgs
        {
            Uid = update.FirebaseUid,
            DisplayName = update.DisplayName,
            PhoneNumber = update.PhoneNumber,
            Disabled = update.Disabled
        }, cancellationToken);
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
