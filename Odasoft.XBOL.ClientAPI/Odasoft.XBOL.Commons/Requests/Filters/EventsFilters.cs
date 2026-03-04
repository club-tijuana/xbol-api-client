namespace Odasoft.XBOL.Commons.Requests.Filters
{
    public class EventsFilters : BaseFilters
    {
        public List<long>? EventCategoryIds { get; set; }
        public long? PerformerId { get; set; }
    }
}
