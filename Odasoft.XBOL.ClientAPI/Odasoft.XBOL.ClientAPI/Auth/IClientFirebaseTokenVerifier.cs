namespace Odasoft.XBOL.ClientAPI.Auth;

public interface IClientFirebaseTokenVerifier
{
    Task<VerifiedFirebaseClientToken> VerifyIdTokenAsync(
        string token,
        CancellationToken cancellationToken);
}
