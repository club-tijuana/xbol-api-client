namespace Odasoft.XBOL.Data.Projections
{
    public class MyOrderProjection
    {
        public long Id { get; set; }
        public string Reference { get; set; } = string.Empty;
        public EventCardProjection? Event { get; set; }
        public BundleCardProjection? Bundle { get; set; }
        public List<MyOrderTicketProjection> Tickets { get; set; } = [];
    }
}
