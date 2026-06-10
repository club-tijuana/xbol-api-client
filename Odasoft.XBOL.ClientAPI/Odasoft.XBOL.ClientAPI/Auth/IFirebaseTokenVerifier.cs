namespace Odasoft.XBOL.ClientAPI.Auth;

public interface IFirebaseTokenVerifier
{
    Task<VerifiedFirebaseToken> VerifyIdTokenAsync(
        string token,
        CancellationToken cancellationToken);
}
