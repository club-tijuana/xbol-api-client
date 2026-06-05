namespace Odasoft.XBOL.Commons.Requests
{
    public class ShareTicketRequest
    {
        public long TicketId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FullPhone { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public long PhoneRegionCodeId { get; set; }
        public bool ApplyToEntireSeason { get; set; }
    }
}
