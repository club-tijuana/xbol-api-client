namespace Odasoft.XBOL.Models
{
    public class EventMedia : BaseModel
    {
        public long EventId { get; set; }
        public Event Event { get; set; } = null!;

        public string MediaType { get; set; } = null!;
        public string Url { get; set; } = null!;
        public int SortOrder { get; set; }
    }
}
