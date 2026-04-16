namespace Odasoft.XBOL.Models
{
    public class VenueMap : BaseModel
    {
        public long VenueId { get; set; }
        public Venue Venue { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string ExternalMapKey { get; set; } = null!;
        public int Capacity { get; set; }
        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public Guid UpdatedBy { get; set; }

        public IList<Event> Events { get; set; } = [];
        public IList<BaseZone> BaseZones { get; set; } = [];
        public IList<Gate> Gates { get; set; } = [];
    }
}
