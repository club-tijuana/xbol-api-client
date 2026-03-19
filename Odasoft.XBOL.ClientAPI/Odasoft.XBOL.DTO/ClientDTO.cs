namespace Odasoft.XBOL.DTO
{
    public class ClientDTO
    {
        public long Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string? BusinessName { get; set; }

        public string Email { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;
        public string PhoneCode { get; set; } = null!;
    }
}
