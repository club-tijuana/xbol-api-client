namespace Odasoft.XBOL.DTO
{
    public class SeasonToRenovateDTO
    {
        public long RelatedOrderId { get; set; }
        public long PreviousSeasonId { get; set; }
        public long SeasonId { get; set; }
        public string SeasonKey { get; set; } = string.Empty;
        public List<MyEventSeatDTO> PreviousSeats { get; set; } = new List<MyEventSeatDTO>();
        public List<SeatDTO>? PreviousSeatPrices { get; set; }
    }
}
