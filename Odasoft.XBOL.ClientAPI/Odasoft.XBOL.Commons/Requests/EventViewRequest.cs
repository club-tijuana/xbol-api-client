namespace Odasoft.XBOL.Commons.Requests
{
    public class EventViewRequest
    {
        public long? EventId { get; set; }
        public string? VisitorId { get; set; }
        public string? Platform { get; set; }
        public string? IpAddress { get; set; }
    }
}
