namespace Odasoft.XBOL.ClientAPI.Auth;

public sealed record VerifiedFirebaseToken(
    string Uid,
    IReadOnlyDictionary<string, object> Claims);
