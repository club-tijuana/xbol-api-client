namespace Odasoft.XBOL.Models
{
    public class BaseZone : BaseModel
    {
        public long VenueMapId { get; set; }
        public VenueMap VenueMap { get; set; } = null!;

        public string Name { get; set; } = null!;
        public long? ExternalZoneKey { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public Guid UpdatedBy { get; set; }
        public DateTimeOffset? DeletedAt { get; set; }

        public IList<BaseSection> BaseSections { get; set; } = [];
    }
}
