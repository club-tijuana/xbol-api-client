namespace Odasoft.XBOL.Data.Projections
{
    public class MyEventsDiagnosticsProjection
    {
        public int PaidOrdersByType { get; set; }
        public int QualifyingTicketOrders { get; set; }
        public int BundlePassItemOrders { get; set; }
        public int OwnedBundlePasses { get; set; }
        public int ActiveOwnedBundlePasses { get; set; }
        public int PublishedActiveBundlePassItemOrders { get; set; }
    }
}
