using Odasoft.XBOL.Commons.Enums;

namespace Odasoft.XBOL.Models
{
    public class Payment : BaseModel
    {
        public long OrderId { get; set; }
        public Order Order { get; set; } = null!;

        public decimal Amount { get; set; }
        public decimal? TenderedAmount { get; set; }
        public decimal? ChangeAmount { get; set; }

        public PaymentType PaymentType { get; set; }

        public string Provider { get; set; } = null!;
        public string ProviderReference { get; set; } = null!;

        public Guid TransactionReference { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public Guid UpdatedBy { get; set; }
    }
}
