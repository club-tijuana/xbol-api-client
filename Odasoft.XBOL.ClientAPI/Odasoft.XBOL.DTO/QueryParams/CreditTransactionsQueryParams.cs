using Odasoft.XBOL.Commons.Enums;

namespace Odasoft.XBOL.DTO.QueryParams
{
    public class CreditTransactionsQueryParams : BaseQueryParams
    {
        public long? CreditAccountId { get; set; }
        public List<PaymentType> PaymentTypes { get; set; } = [];
        public DateTimeOffset? StartDate { get; set; }
        public DateTimeOffset? EndDate { get; set; }
    }
}
