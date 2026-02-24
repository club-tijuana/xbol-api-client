using Odasoft.XBOL.Commons.Enums;

namespace Odasoft.XBOL.Models
{
    public class Ticket : BaseModel
    {
        public long EventScheduleId { get; set; }
        public EventSchedule EventSchedule { get; set; } = null!;

        public long EventSectionId { get; set; }
        public EventSection EventSection { get; set; } = null!;

        public long? SeasonPassEventTicketId { get; set; }
        public SeasonPassEventTicket? SeasonPassEventTicket { get; set; }

        public long EventSeatId { get; set; }
        public EventSeat EventSeat { get; set; } = null!;

        public long? InventoryBatchId { get; set; }
        public InventoryBatch? InventoryBatch { get; set; }

        public long? OriginalClientId { get; set; }
        public Client? OriginalClient { get; set; }

        public long? CurrentClientId { get; set; }
        public Client? CurrentClient { get; set; }

        public long? OriginalOrderId { get; set; }
        public Order? OriginalOrder { get; set; }

        public string TicketCode { get; set; } = null!;
        public string TicketType { get; set; } = null!;
        public string PrivateToken { get; set; } = null!;

        public string SectionLabelSnapshot { get; set; } = null!;
        public string SeatLabelSnapshot { get; set; } = null!;

        public decimal PricePaid { get; set; }

        public TicketStatus Status { get; set; }

        public DateTimeOffset? LastScanAt { get; set; }
        public string? LastScanResult { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public Guid UpdatedBy { get; set; }
    }
}
