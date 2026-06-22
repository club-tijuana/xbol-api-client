using Odasoft.XBOL.Commons.Enums;

namespace Odasoft.XBOL.Models
{
    public class Payment : BaseModel
    {
        public long OrderId { get; set; }
        public Order Order { get; set; } = null!;

        public CurrencyType Currency { get; set; }
        public decimal Amount { get; set; }
        public decimal AmountMXN { get; set; }
        public decimal? ReceivedAmount { get; set; }
        public decimal? ReceivedAmountMXN { get; set; }
        public long ExchangeRateId { get; set; }
        public decimal ExchangeRate { get; set; }

        public PaymentType PaymentType { get; set; }
        public PaymentStatus PaymentStatus { get; set; }

        public string Provider { get; set; } = null!;
        public string ProviderReference { get; set; } = null!;

        public Guid TransactionReference { get; set; }
        public DateTimeOffset AppliedAt { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public Guid UpdatedBy { get; set; }
    }
}
