namespace Odasoft.XBOL.Models
{
    public class BundlePassEventTicket : BaseModel
    {
        public long BundlePassId { get; set; }
        public BundlePass BundlePass { get; set; } = null!;

        public long TicketId { get; set; }
        public Ticket Ticket { get; set; } = null!;
    }
}