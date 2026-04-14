namespace Odasoft.XBOL.Models
{
    public class EventTag : BaseModel
    {
        public long EventId { get; set; }
        public Event Event { get; set; } = null!;

        public long TagId { get; set; }
        public Tag Tag { get; set; } = null!;
    }
}
