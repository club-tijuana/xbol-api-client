namespace Odasoft.XBOL.Models
{
    public class PromoCode : BaseModel
    {
        public string Code { get; set; } = null!;
        public string DiscountType { get; set; } = null!;

        public decimal DiscountAmount { get; set; }

        public int MaxUses { get; set; }
        public int PerUserLimit { get; set; }

        public DateTimeOffset ValidFrom { get; set; }
        public DateTimeOffset ValidTo { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public Guid UpdatedBy { get; set; }

        public IList<PromoCodeRedemption> PromoCodeRedemptions { get; set; } = [];
    }
}
