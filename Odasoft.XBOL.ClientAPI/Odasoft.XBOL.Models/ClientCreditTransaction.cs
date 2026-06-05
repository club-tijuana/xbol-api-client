using Odasoft.XBOL.Commons.Enums;

namespace Odasoft.XBOL.Models
{
    public class ClientCreditTransaction : BaseModel
    {
        public long ClientCreditAccountId { get; set; }
        public ClientCreditAccount ClientCreditAccount { get; set; } = null!;

        // Core data
        public CreditTransactionType TransactionType { get; set; }

        public PaymentType PaymentType { get; set; }

        public decimal Amount { get; set; }
        public DateTimeOffset TransactionDate { get; set; }

        // The "Localizer" / Reference
        public string Description { get; set; } = "";

        public string ReferenceId { get; set; } = "";

        // Audit
        public DateTimeOffset CreatedAt { get; set; }

        public Guid CreatedBy { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public Guid UpdatedBy { get; set; }
    }
}
