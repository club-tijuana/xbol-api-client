namespace Odasoft.XBOL.Commons.Requests.Filters
{
    public class SearchEventsFilters : BaseFilters
    {
        public List<long>? EventCategoryIds { get; set; }
        public long? PerformerId { get; set; }
        public bool? TrendingEvents { get; set; }
    }
}
