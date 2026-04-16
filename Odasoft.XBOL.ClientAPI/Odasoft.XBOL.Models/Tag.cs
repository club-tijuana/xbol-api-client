namespace Odasoft.XBOL.Models
{
    public class Tag : BaseModel
    {
        public long TagTypeId { get; set; }
        public TagType TagType { get; set; } = null!;

        public string Name { get; set; } = null!;
        public string Slug { get; set; } = null!;

        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public Guid UpdatedBy { get; set; }

        public IList<SeasonTag> SeasonTags { get; set; } = [];

        public IList<EventTag> EventTags { get; set; } = [];
    }
}
