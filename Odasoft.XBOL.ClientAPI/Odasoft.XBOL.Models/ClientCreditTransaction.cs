using Odasoft.XBOL.Commons.Enums;

namespace Odasoft.XBOL.Models
{
    public class ClientCreditTransaction : BaseModel
    {
        public long ClientCreditAccountId { get; set; }
        public ClientCreditAccount ClientCreditAccount { get; set; } = null!;

        public decimal Amount { get; set; }
        public CreditTransactionType TransactionType { get; set; }

        public CreditTransactionStatus Status { get; set; }
    }
}
