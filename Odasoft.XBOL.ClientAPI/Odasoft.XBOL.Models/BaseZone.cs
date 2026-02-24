namespace Odasoft.XBOL.Models
{
    public class BaseZone : BaseModel
    {
        public long VenueMapId { get; set; }
        public VenueMap VenueMap { get; set; } = null!;

        public string Name { get; set; } = null!;

        public IList<BaseSection> BaseSections { get; set; } = [];
    }
}
