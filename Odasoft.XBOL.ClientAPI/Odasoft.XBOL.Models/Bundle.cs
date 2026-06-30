using Odasoft.XBOL.Commons.Enums;

namespace Odasoft.XBOL.Models
{
    public class Bundle : Event
    {
        public BundleType BundleType { get; set; }
        public BundlePricingType BundlePricingType { get; set; }

        public string? Code { get; set; }
        public string? ExternalKey { get; set; }

        public DateTimeOffset? StartDate { get; set; }
        public DateTimeOffset? EndDate { get; set; }
        public DateTimeOffset? PublishedDate { get; set; }
        public DateTimeOffset? OnSaleDate { get; set; }
        public DateTimeOffset? PreSaleDate { get; set; }
        public DateTimeOffset? OffSaleDate { get; set; }
        public DateTimeOffset? RenewalStartDate { get; set; }
        public DateTimeOffset? RenewalEndDate { get; set; }

        public long? PreviousBundleId { get; set; }
        public Bundle? PreviousBundle { get; set; }

        public int? HoldExpirationInMinutes { get; set; }

        public IList<BundleEventSchedule> BundleEventSchedules { get; set; } = [];
        public IList<BundleSection> BundleSections { get; set; } = [];
        public IList<BundleTag> BundleTags { get; set; } = [];
        public IList<BundlePass> BundlePasses { get; set; } = [];
    }
}