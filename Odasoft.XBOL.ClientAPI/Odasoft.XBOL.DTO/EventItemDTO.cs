using Odasoft.XBOL.Commons.Enums;

namespace Odasoft.XBOL.DTO
{
    public class EventItemDTO
    {
        public long Id { get; set; }
        public string BannerImageUrl { get; set; } = string.Empty;
        public string PosterImageUrl { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public DateTimeOffset StartDate { get; set; }
        public string Location { get; set; } = string.Empty;
        public EventCategory Category { get; set; }
    }
}
