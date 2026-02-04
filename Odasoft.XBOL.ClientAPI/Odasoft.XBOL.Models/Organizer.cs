namespace Odasoft.XBOL.Models
{
    public class Organizer : BaseModel
    {
        public string Name { get; set; } = null!;
        public string ContactEmail { get; set; } = null!;
        public string ContactPhone { get; set; } = null!;
        public string WebsiteUrl { get; set; } = null!;
        public string LogoUrl { get; set; } = null!;
        public string BannerUrl { get; set; } = null!;

        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public Guid UpdatedBy { get; set; }

        public IList<Event> Events { get; set; } = [];
        public IList<OrganizerMember> OrganizerMembers { get; set; } = [];
    }
}
