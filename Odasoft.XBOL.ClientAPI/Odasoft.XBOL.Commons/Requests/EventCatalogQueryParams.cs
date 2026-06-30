using Odasoft.XBOL.Commons.Enums;

namespace Odasoft.XBOL.Commons.Requests
{
    public class EventCatalogQueryParams
    {
        public string SearchTerm { get; set; } = "";
        public EventCatalogItemType? ItemType { get; set; }
        public BundleType? BundleType { get; set; }
        public EventStatus? Status { get; set; }
        public string? Venue { get; set; }
        public DateTimeOffset? StartDate { get; set; }
        public DateTimeOffset? EndDate { get; set; }
        public bool? Upcoming { get; set; }
        public bool? BuyableOnly { get; set; }
        public string? SortBy { get; set; }
        public bool Descending { get; set; } = true;
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
