namespace Odasoft.XBOL.Models
{
    public class PromoCodeRedemption : BaseModel
    {
        public long PromoCodeId { get; set; }
        public PromoCode PromoCode { get; set; } = null!;

        public Guid? UserId { get; set; }
        public User? User { get; set; }

        public long OrderId { get; set; }
        public Order Order { get; set; } = null!;

        public DateTimeOffset RedeemedAt { get; set; }
    }
}
