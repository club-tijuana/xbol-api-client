namespace Odasoft.XBOL.DTO
{
    public class MyEventDTO
    {
        public long OrderId { get; set; }
        public long EventId { get; set; }
        public string EventImage { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public DateTimeOffset StartDate { get; set; }
        public string Location { get; set; } = string.Empty;
        public bool isSeasonPass { get; set; }
        public bool isPastEvent { get; set; }
    }
}
