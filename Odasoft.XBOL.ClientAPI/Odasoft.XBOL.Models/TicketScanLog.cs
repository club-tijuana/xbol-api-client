namespace Odasoft.XBOL.Models
{
    public class TicketScanLog : BaseModel
    {
        public long TicketId { get; set; }
        public Ticket Ticket { get; set; } = null!;

        public long DeviceId { get; set; }
        public Device Device { get; set; } = null!;

        public Guid? StaffUserId { get; set; }

        public DateTimeOffset ScannedAt { get; set; }
        public string Result { get; set; } = null!;
    }
}
