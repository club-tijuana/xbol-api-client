using Odasoft.XBOL.Commons.Enums;

namespace Odasoft.XBOL.Models
{
    public class Bundle : BaseModel
    {
        public long? VenueMapId { get; set; }
        public VenueMap? VenueMap { get; set; }
        public string Name { get; set; } = null!;
        public string? BannerImageUrl { get; set; }
        public string? PosterImageUrl { get; set; }
        public EventStatus Status { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public BundleType BundleType { get; set; }
        public BundlePricingType BundlePricingType { get; set; }
        public string? Code { get; set; }
        public string? ExternalKey { get; set; }
        public DateTimeOffset? StartDate { get; set; }
        public DateTimeOffset? EndDate { get; set; }
        public DateTimeOffset? OnSaleDate { get; set; }
        public DateTimeOffset? PreSaleDate { get; set; }
        public DateTimeOffset? OffSaleDate { get; set; }
        public DateTimeOffset? RenewalStartDate { get; set; }
        public DateTimeOffset? RenewalEndDate { get; set; }
        public long? PreviousBundleId { get; set; }
        public Bundle? PreviousBundle { get; set; }
        public IList<BundleEventSchedule> BundleEventSchedules { get; set; } = [];
        public IList<BundleSection> BundleSections { get; set; } = [];
    }
}
