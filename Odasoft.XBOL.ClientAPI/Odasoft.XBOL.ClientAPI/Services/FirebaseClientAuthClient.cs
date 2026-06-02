using FirebaseAdmin.Auth;

namespace Odasoft.XBOL.ClientAPI.Services;

public sealed class FirebaseClientAuthClient(FirebaseAuth firebaseAuth) : IFirebaseClientAuthClient
{
    public async Task<FirebaseClientUser> CreateUserAsync(
        CreateFirebaseClientUserRequest user,
        CancellationToken cancellationToken)
    {
        var createdUser = await firebaseAuth.CreateUserAsync(new UserRecordArgs
        {
            Email = user.Email,
            Password = user.Password,
            DisplayName = user.DisplayName,
            EmailVerified = user.EmailVerified,
            Disabled = user.Disabled
        }, cancellationToken);

        return new FirebaseClientUser(
            createdUser.Uid,
            createdUser.Email,
            createdUser.EmailVerified,
            createdUser.DisplayName,
            createdUser.PhoneNumber);
    }

    public Task DeleteUserAsync(string firebaseUid, CancellationToken cancellationToken)
    {
        return firebaseAuth.DeleteUserAsync(firebaseUid, cancellationToken);
    }

    public Task<string> CreateCustomTokenAsync(string firebaseUid, CancellationToken cancellationToken)
    {
        return firebaseAuth.CreateCustomTokenAsync(firebaseUid, cancellationToken);
    }
}
