using FirebaseAdmin.Auth;

namespace Odasoft.XBOL.ClientAPI.Auth;

public sealed class FirebaseTokenVerifier(FirebaseAuth firebaseAuth) : IFirebaseTokenVerifier
{
    public async Task<VerifiedFirebaseToken> VerifyIdTokenAsync(
        string token,
        CancellationToken cancellationToken)
    {
        var decoded = await firebaseAuth.VerifyIdTokenAsync(token, cancellationToken);
        return new VerifiedFirebaseToken(
            decoded.Uid,
            decoded.TenantId,
            decoded.Claims);
    }
}
