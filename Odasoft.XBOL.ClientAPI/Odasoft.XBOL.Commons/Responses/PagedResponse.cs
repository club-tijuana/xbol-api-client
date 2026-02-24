namespace Odasoft.XBOL.Commons.Responses
{
    public class PagedResponse<T>
    {
        public IReadOnlyList<T> Items { get; set; } = [];

        public int CurrentPage { get; set; }
        public int PageSize { get; set; }

        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
    }
}
