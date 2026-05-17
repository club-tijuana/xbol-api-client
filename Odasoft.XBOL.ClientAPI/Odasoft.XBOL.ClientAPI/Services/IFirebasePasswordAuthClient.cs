namespace Odasoft.XBOL.ClientAPI.Services;

public interface IFirebasePasswordAuthClient
{
    Task<FirebasePasswordAuthResult> SignUpAsync(
        string email,
        string password,
        CancellationToken cancellationToken);
}
