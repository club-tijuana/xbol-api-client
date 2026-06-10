namespace Odasoft.XBOL.ClientAPI.Auth;

public sealed record VerifiedFirebaseToken(
    string Uid,
    string? TenantId,
    IReadOnlyDictionary<string, object> Claims);
