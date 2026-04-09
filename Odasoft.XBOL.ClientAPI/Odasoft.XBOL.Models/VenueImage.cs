using Odasoft.XBOL.Commons.Enums;

namespace Odasoft.XBOL.Models
{
    public class VenueImage : BaseModel
    {
        public long VenueId { get; set; }
        public Venue Venue { get; set; } = null!;
        public ImageType ImageType { get; set; }
        public byte[] Content { get; set; } = [];
        public string ContentType { get; set; } = "";
        public string FileName { get; set; } = "";
        public int Order { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public Guid UpdatedBy { get; set; }
    }
}
