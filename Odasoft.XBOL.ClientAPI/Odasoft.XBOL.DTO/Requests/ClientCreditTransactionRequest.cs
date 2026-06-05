using Odasoft.XBOL.Commons.Enums;

namespace Odasoft.XBOL.DTO.Requests
{
    public class ClientCreditTransactionRequest
    {
        public CreditTransactionType TransactionType { get; set; }
        public PaymentType PaymentType { get; set; }
        public decimal Amount { get; set; }
        public DateTimeOffset TransactionDate { get; set; }
        public string ReferenceId { get; set; } = "";
        public string Description { get; set; } = "";
    }
}
