namespace Odasoft.XBOL.DTO
{
    public class ClientDTO
    {
        public long Id { get; set; }
        public string FirebaseUid { get; set; } = string.Empty;
        public string UserId => FirebaseUid;
        public string FullName { get; set; } = null!;
        public string? BusinessName { get; set; }

        public string Email { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;
        public long? PhoneRegionCodeId { get; set; }
        public string PhoneCode { get; set; } = null!;
    }
}
