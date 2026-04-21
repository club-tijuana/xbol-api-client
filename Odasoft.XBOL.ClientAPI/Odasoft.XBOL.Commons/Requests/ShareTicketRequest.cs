namespace Odasoft.XBOL.Commons.Requests
{
    public class ShareTicketRequest
    {
        public long TicketId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string PhoneCode { get; set; } = string.Empty;
        public string PhoneIsoCode { get; set; } = string.Empty;
        public bool ApplyToEntireSeason { get; set; }
    }
}
