using Odasoft.XBOL.Commons.Enums;

namespace Odasoft.XBOL.Models
{
    public class BaseSection : BaseModel
    {
        public long BaseZoneId { get; set; }
        public BaseZone BaseZone { get; set; } = null!;

        public string Name { get; set; } = null!;
        public SectionType SectionType { get; set; }

        public IList<BaseRow> BaseRows { get; set; } = [];
    }
}
