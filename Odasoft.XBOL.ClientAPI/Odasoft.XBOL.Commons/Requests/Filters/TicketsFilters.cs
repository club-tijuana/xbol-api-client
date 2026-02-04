namespace Odasoft.XBOL.Commons.Requests.Filters
{
    public class TicketsFilters : BaseFilters
    {
        public string? TicketType { get; set; }
        public long? EventId { get; set; }
    }
}
