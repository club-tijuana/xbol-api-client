namespace Odasoft.XBOL.Data.Projections
{
    public class PagedOrdersProjection
    {
        public int TotalCount { get; set; }
        public List<MyOrderProjection> Orders { get; set; } = [];
    }
}
