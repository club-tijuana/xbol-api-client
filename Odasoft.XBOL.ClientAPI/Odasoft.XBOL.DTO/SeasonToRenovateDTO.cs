namespace Odasoft.XBOL.DTO
{
    public class SeasonToRenovateDTO
    {
        public long RelatedOrderId { get; set; }
        public long PreviousSeasonId { get; set; }
        public long SeasonId { get; set; }
        public List<MyEventSeatDTO> PreviousSeats { get; set; } = new List<MyEventSeatDTO>();
    }
}
