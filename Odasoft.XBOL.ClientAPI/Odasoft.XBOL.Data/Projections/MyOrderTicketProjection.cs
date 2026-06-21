namespace Odasoft.XBOL.Data.Projections
{
    public class MyOrderTicketProjection
    {
        public long EventScheduleId { get; set; }
        public DateTimeOffset StartDateTime { get; set; }
        public DateTimeOffset EndDateTime { get; set; }
        public long EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public string? LegacyPosterUrl { get; set; }
        public string? BannerUrl { get; set; }
        public string Location { get; set; } = string.Empty;
        public string TicketType { get; set; } = string.Empty;
        public long? SeasonId { get; set; }
        public string SeasonName { get; set; } = string.Empty;
        public string Source { get; set; } = "Ticket";
        public bool CanViewTickets { get; set; } = true;
    }
}
