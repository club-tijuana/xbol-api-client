namespace Odasoft.XBOL.Models
{
    public class SeasonSeat : BaseModel
    {
        public long SeasonSectionId { get; set; }
        public SeasonSection SeasonSection { get; set; } = null!;

        public long BaseSeatId { get; set; }
        public BaseSeat BaseSeat { get; set; } = null!;

        public string ExternalSeatObjectKey { get; set; } = null!;
        public bool ForSale { get; set; } = true;
    }
}
