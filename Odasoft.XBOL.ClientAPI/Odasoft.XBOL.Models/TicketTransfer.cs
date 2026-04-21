using Odasoft.XBOL.Commons.Enums;

namespace Odasoft.XBOL.Models
{
    public class TicketTransfer : BaseModel
    {
        public long TicketId { get; set; }
        public Ticket Ticket { get; set; } = null!;

        public long? OrderItemId { get; set; }
        public OrderItem? OrderItem { get; set; }

        public long FromClientId { get; set; }
        public Client FromClient { get; set; } = null!;

        public long? ToClientId { get; set; }
        public Client? ToClient { get; set; }

        public decimal? Price { get; set; }

        public Guid Reference { get; set; }

        public TicketTransferStatus Status { get; set; }
        public TicketTransferReason Reason { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
    }
}
