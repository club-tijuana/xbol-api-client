namespace Odasoft.XBOL.ClientAPI.Auth;

public sealed record VerifiedFirebaseClientToken(
    string Uid,
    string? TenantId,
    IReadOnlyDictionary<string, object> Claims);
