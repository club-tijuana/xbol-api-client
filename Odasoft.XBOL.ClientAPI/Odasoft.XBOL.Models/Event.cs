using Odasoft.XBOL.Commons.Enums;

namespace Odasoft.XBOL.Models
{
    public class Event : BaseModel
    {
        public long VenueMapId { get; set; }
        public VenueMap VenueMap { get; set; } = null!;

        public long OrganizerId { get; set; }

        public long? SeasonId { get; set; }
        public Season? Season { get; set; }

        public string Name { get; set; } = null!;
        public string Subtitle { get; set; } = null!;
        public EventCategory Category { get; set; }

        public string ShortDescription { get; set; } = null!;
        public string LongDescription { get; set; } = null!;

        public string BannerImageUrl { get; set; } = null!;
        public string PosterImageUrl { get; set; } = null!;
        public string LandingUrl { get; set; } = null!;

        public EventStatus Status { get; set; }

        public IList<EventSchedule> Schedules { get; set; } = [];
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public Guid UpdatedBy { get; set; }

        public long? PerformerId { get; set; }
        public Performer? Performer { get; set; }
    }
}
