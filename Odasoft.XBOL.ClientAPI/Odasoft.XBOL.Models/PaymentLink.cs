using Odasoft.XBOL.Commons.Enums;

namespace Odasoft.XBOL.Models
{
    public class PaymentLink : BaseModel
    {
        public long OrderId { get; set; }
        public Order Order { get; set; } = null!;
        public Guid? CreatedById { get; set; }
        public User? CreatedBy { get; set; }
        public string Code { get; set; } = null!;
        public string Reference { get; set; } = null!;
        public PaymentLinkStatus Status { get; set; }
        public DateTimeOffset ActivationDateTime { get; set; }
        public DateTimeOffset ExpirationDateTime { get; set; }
        public DateTimeOffset? PaidAt { get; set; }
        public DateTimeOffset? CancelledAt { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
