namespace Odasoft.XBOL.Models
{
    public class BundleSeat : BaseModel
    {
        public long BundleSectionId { get; set; }
        public BundleSection BundleSection { get; set; } = null!;
        public long BaseSeatId { get; set; }
        public BaseSeat BaseSeat { get; set; } = null!;
        public string ExternalSeatObjectKey { get; set; } = null!;
        public bool ForSale { get; set; } = true;
        public IList<BundlePass> BundlePasses { get; set; } = [];
    }
}