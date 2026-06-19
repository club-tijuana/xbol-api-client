namespace Odasoft.XBOL.Models
{
    public class BundleSection : BaseModel
    {
        public long BundleId { get; set; }
        public Bundle Bundle { get; set; } = null!;
        public long BaseSectionId { get; set; }
        public BaseSection BaseSection { get; set; } = null!;
        public string DisplayName { get; set; } = null!;
        public int TotalSeats { get; set; }
        public int AvailableSeats { get; set; }
        public IList<BundleSeat> BundleSeats { get; set; } = [];
    }
}
