namespace Odasoft.XBOL.Data.Projections
{
    public class EventCardProjection
    {
        public string Name { get; set; } = "";
        public string? BannerUrl { get; set; }
        public string? LegacyPosterUrl { get; set; }
        public string Location { get; set; } = "";
        public DateTimeOffset StartDate { get; set; }
        public DateTimeOffset EndDate { get; set; }
    }
}
