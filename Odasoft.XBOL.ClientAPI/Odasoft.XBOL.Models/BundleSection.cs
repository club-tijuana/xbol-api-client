namespace Odasoft.XBOL.Models
{
    public class BundleSection : BaseModel
    {
        public long BundleId { get; set; }
        public Bundle Bundle { get; set; } = null!;
        public long BaseSectionId { get; set; }
        public string DisplayName { get; set; } = null!;
        public decimal? Price { get; set; }
        public int TotalSeats { get; set; }
        public int AvailableSeats { get; set; }
    }
}
