using Odasoft.XBOL.Commons.Enums;

namespace Odasoft.XBOL.Models
{
    public class Event : BaseModel
    {
        public long VenueMapId { get; set; }
        public VenueMap VenueMap { get; set; } = null!;

        public long? OrganizerId { get; set; }
        public Organizer? Organizer { get; set; }

        public long? SeasonId { get; set; }
        public Season? Season { get; set; }

        public string Name { get; set; } = null!;
        public string? Subtitle { get; set; }
        public string? ShortDescription { get; set; }
        public string? LongDescription { get; set; }

        public string? BannerImageUrl { get; set; }
        public string? PosterImageUrl { get; set; }
        public string? LandingUrl { get; set; }
        public AgeRestriction? AgeRestriction { get; set; }
        public string? SecurityPolicies { get; set; }
        public string? AdditionalComments { get; set; }

        public EventStatus Status { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public Guid UpdatedBy { get; set; }

        public IList<EventSchedule> Schedules { get; set; } = [];
        public IList<EventMedia> Media { get; set; } = [];
        public IList<EventTag> Tags { get; set; } = [];
        public IList<EventCategory> Categories { get; set; } = new List<EventCategory>();

        public long? PerformerId { get; set; }
        public Performer? Performer { get; set; }

        public long ViewCount { get; set; }
    }
}
