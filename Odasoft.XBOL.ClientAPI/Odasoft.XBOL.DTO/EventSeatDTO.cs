namespace Odasoft.XBOL.DTO
{
    public class EventSeatDTO
    {
        public long Id { get; set; }
        public long EventSectionId { get; set; }
        public long BaseSeatId { get; set; }
        public decimal? PriceOverride { get; set; }
        public string ExternalSeatObjectKey { get; set; } = string.Empty;
    }
}
