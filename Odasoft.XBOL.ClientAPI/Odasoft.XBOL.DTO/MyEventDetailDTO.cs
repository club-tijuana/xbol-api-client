namespace Odasoft.XBOL.DTO
{
    public class MyEventDetailDTO
    {
        public long OrderId { get; set; }
        public long EventId { get; set; }
        public string EventKey { get; set; } = string.Empty;
        public string EventImage { get; set; } = string.Empty;
        public string Folio { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public DateTimeOffset Date { get; set; }
        public string Location { get; set; } = string.Empty;
        public decimal SubTotal { get; set; }
        public decimal TotalFees { get; set; }
        public decimal TotalTaxes { get; set; }
        public decimal Total { get; set; }
        public string Currency { get; set; } = string.Empty;
        public List<MyEventSeatDTO> Seats { get; set; } = new List<MyEventSeatDTO>();
        public List<string> SelectedSeats { get; set; } = new List<string>();
        public List<OrderFeeDTO> Fees { get; set; } = new List<OrderFeeDTO>();
    }
}
