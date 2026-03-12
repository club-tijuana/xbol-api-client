namespace Odasoft.XBOL.DTO
{
    public class EventSectionDTO
    {
        public long Id { get; set; }
        public long EventSectionId { get; set; }
        public long BaseSectionId { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public decimal? Price { get; set; }
    }
}
