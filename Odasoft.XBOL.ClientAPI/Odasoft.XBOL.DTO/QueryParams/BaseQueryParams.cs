using Odasoft.XBOL.Commons;

namespace Odasoft.XBOL.DTO.QueryParams
{
    public class BaseQueryParams
    {
        public string SearchTerm { get; set; } = "";
        public string SortBy { get; set; } = "";
        public bool Descending { get; set; } = false;
        public int Page { get; set; } = QueryParamsConstants.DEFAULT_PAGE;
        public int PageSize { get; set; } = QueryParamsConstants.DEFAULT_PAGE_SIZE;
    }
}
