namespace Odasoft.XBOL.DTO.Requests
{
    public class RegisterRequest
    {
        public string? Identifier { get; set; }
        public string? IdentifierCountryCode { get; set; }
        public string? Password { get; set; }
        public string? FullName { get; set; }
    }
}
