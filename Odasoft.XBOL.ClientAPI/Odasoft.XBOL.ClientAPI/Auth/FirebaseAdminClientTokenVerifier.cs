using FirebaseAdmin.Auth.Multitenancy;

namespace Odasoft.XBOL.ClientAPI.Auth;

public sealed class FirebaseAdminClientTokenVerifier(TenantAwareFirebaseAuth tenantAuth) : IClientFirebaseTokenVerifier
{
    public async Task<VerifiedFirebaseClientToken> VerifyIdTokenAsync(
        string token,
        CancellationToken cancellationToken)
    {
        var decoded = await tenantAuth.VerifyIdTokenAsync(token, cancellationToken);
        return new VerifiedFirebaseClientToken(decoded.Uid, decoded.TenantId, decoded.Claims);
    }
}
