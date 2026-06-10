using Odasoft.XBOL.Commons.Enums;

namespace Odasoft.XBOL.DTO.Results
{
    public class CreditTransactionResult
    {
        public long Id { get; set; }
        public DateTimeOffset PaymentDate { get; set; }
        public string ReferenceId { get; set; } = "";
        public decimal Amount { get; set; }
        public PaymentType PaymentType { get; set; }
        public CreditTransactionType TransactionType { get; set; }
        public string ReceivedBy { get; set; } = "";
    }
}
