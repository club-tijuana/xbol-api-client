namespace Odasoft.XBOL.DTO
{
    public class BundleToRenovateDTO
    {
        public long RelatedOrderId { get; set; }
        public long PreviousBundleId { get; set; }
        public long BundleId { get; set; }
        public string BundleKey { get; set; } = string.Empty;
        public List<MyEventSeatDTO> PreviousSeats { get; set; } = new List<MyEventSeatDTO>();
        public List<SeatDTO>? PreviousSeatPrices { get; set; }
    }
}
