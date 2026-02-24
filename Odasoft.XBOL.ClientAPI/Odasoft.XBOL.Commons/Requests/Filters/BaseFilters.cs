namespace Odasoft.XBOL.Commons.Requests.Filters
{
    public class BaseFilters
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public string? SortBy { get; set; }
        public bool? SortDesc { get; set; }
        public string? TextFilter { get; set; }
    }
}
