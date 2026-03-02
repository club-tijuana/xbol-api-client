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
        public decimal Total { get; set; }
        public string Currency { get; set; } = string.Empty;
        public required List<OrderEventDTO> Events { get; set; }
    }
}
