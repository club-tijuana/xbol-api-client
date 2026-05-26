namespace Odasoft.XBOL.DTO.Responses
{
    public class RegisterResponse
    {
        public string FirebaseUid { get; set; } = string.Empty;
        public string? CustomToken { get; set; }
        public ClientDTO Client { get; set; } = null!;
        public string OnboardingStatus { get; set; } = "linked";
        public string VerificationStatus { get; set; } = "pending";
    }
}
