namespace Odasoft.XBOL.Models
{
    public class EventViews : BaseModel
    {
        public long EventId { get; set; }
        public Event? Event { get; set; }
        public string VisitorId { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string Platform { get; set; } = string.Empty;
        public DateTimeOffset ViewedAt { get; set; }
    }
}
