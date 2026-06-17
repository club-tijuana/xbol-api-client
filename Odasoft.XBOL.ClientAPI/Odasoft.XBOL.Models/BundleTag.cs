namespace Odasoft.XBOL.Models
{
    public class BundleTag : BaseModel
    {
        public long BundleId { get; set; }
        public Bundle Bundle { get; set; } = null!;

        public long TagId { get; set; }
        public Tag Tag { get; set; } = null!;
    }
}
