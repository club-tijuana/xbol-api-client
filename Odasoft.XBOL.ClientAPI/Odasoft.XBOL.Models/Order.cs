using Odasoft.XBOL.Commons.Enums;

namespace Odasoft.XBOL.Models
{
    public class Order : BaseModel
    {
        public long ClientId { get; set; }
        public Client Client { get; set; } = null!;

        public Guid? UserId { get; set; }
        public User? User { get; set; }

        public string Reference { get; set; } = null!;

        public decimal SubTotal { get; set; }
        public decimal TotalFees { get; set; }
        public decimal TotalTaxes { get; set; }
        public decimal Discount { get; set; }
        public decimal Total { get; set; }

        public OrderStatus Status { get; set; }
        public OrderType OrderType { get; set; }
        public SaleChannel SaleChannel { get; set; }
        public DateTimeOffset PaidAt { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public Guid UpdatedBy { get; set; }

        public long? RelatedOrderId { get; set; }
        public Order? RelatedOrder { get; set; }

        public PaymentLink? PaymentLink { get; set; }

        public IList<OrderItem> Items { get; set; } = [];
        public IList<OrderFee> Fees { get; set; } = [];
        public IList<OrderTax> Taxes { get; set; } = [];
        public IList<Payment> Payments { get; set; } = [];
        public IList<Ticket> Tickets { get; set; } = [];
        public IList<PromoCodeRedemption> PromoRedemptions { get; set; } = [];
        public IList<OrderActionLog> ActionLogs { get; set; } = [];
        public IList<OrderTag> OrderTags { get; set; } = [];
    }
}