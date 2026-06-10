using Odasoft.XBOL.Commons.Enums;

namespace Odasoft.XBOL.DTO.Responses
{
    public class OrderToPayResponse
    {
        public long Id { get; set; }
        public string Reference { get; set; } = null!;
        public decimal SubTotal { get; set; }
        public decimal TotalFees { get; set; }
        public decimal TotalTaxes { get; set; }
        public decimal Discount { get; set; }
        public decimal Total { get; set; }
        public OrderStatus Status { get; set; }
        public OrderType OrderType { get; set; }
        public IList<OrderFeeDTO> Fees { get; set; } = [];
        public IList<OrderTaxDTO> Taxes { get; set; } = [];
        public ItemToPayDTO? ItemToPay { get; set; }
        public List<TicketDTO> Tickets { get; set; } = new List<TicketDTO>();
    }
}
