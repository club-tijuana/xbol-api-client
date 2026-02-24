namespace Odasoft.XBOL.Models
{
    public class SeasonPassEventTicket : BaseModel
    {
        public long SeasonPassId { get; set; }
        public SeasonPass SeasonPass { get; set; } = null!;

        public long TicketId { get; set; }
        public Ticket Ticket { get; set; } = null!;
    }
}
