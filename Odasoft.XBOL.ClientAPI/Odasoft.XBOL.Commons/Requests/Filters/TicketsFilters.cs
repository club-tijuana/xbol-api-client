using Odasoft.XBOL.Commons.Enums;

namespace Odasoft.XBOL.Commons.Requests.Filters
{
    public class TicketsFilters : BaseFilters
    {
        public OrderType? OrderType { get; set; }
        public long? OrderId { get; set; }
    }
}
