using Odasoft.XBOL.Commons.Enums;

namespace Odasoft.XBOL.DTO
{
    public class OrderDTO
    {
        public long Id { get; set; }
        public string Folio { get; set; } = string.Empty;
        public OrderType OrderType { get; set; }
        public decimal SubTotal { get; set; }
        public decimal TotalFees { get; set; }
        public decimal TotalTaxes { get; set; }
        public decimal Discount { get; set; }
        public decimal Total { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string ItemLocation { get; set; } = string.Empty;
        public string ItemKey { get; set; } = string.Empty;
        public List<MyEventSeatDTO> ItemSeats { get; set; } = new List<MyEventSeatDTO>();
        public List<SeatDTO> ItemSeatsLabels { get; set; } = new List<SeatDTO>();
        public string ItemPosterImageUrl { get; set; } = string.Empty;
        public DateTimeOffset? ItemStartDate { get; set; }
    }
}
