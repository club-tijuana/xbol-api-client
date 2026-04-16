namespace Odasoft.XBOL.Models
{
    public class Device : BaseModel
    {
        public string DeviceIdentifier { get; set; } = null!;
        public string DeviceType { get; set; } = null!;
        public string Status { get; set; } = null!;

        public DateTimeOffset LastSeenAt { get; set; }

        public IList<TicketScanLog> TicketScanLogs { get; set; } = [];
    }
}
