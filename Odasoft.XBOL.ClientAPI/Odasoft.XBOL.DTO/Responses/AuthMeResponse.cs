namespace Odasoft.XBOL.DTO.Responses
{
    public class AuthMeResponse
    {
        public string FirebaseUid { get; set; } = string.Empty;
        public string? Email { get; set; }
        public bool EmailVerified { get; set; }
        public string? PhoneNumber { get; set; }
        public string? SignInProvider { get; set; }
        public ClientDTO? Client { get; set; }
        public string OnboardingStatus { get; set; } = "unlinked";
        public string VerificationStatus { get; set; } = "pending";
    }
}
