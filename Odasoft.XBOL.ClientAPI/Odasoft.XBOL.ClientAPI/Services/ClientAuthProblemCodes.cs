namespace Odasoft.XBOL.ClientAPI.Services;

public static class ClientAuthProblemCodes
{
    public const string InvalidRegistration = "invalid_registration";
    public const string FirebaseEmailExists = "firebase_email_exists";
    public const string ClientIdentityConflict = "client_identity_conflict";
    public const string ClaimTokenInvalid = "claim_token_invalid";
    public const string ClaimTokenExpired = "claim_token_expired";
    public const string ClientAlreadyLinked = "client_already_linked";
    public const string UnlinkedClientProfile = "unlinked_client_profile";
    public const string VerificationRequired = "verification_required";
}
