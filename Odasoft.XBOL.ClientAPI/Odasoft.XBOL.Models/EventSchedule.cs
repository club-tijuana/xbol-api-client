using Odasoft.XBOL.Commons.Enums;

namespace Odasoft.XBOL.Models
{
    public class EventSchedule : BaseModel
    {
        public long EventId { get; set; }
        public Event Event { get; set; } = null!;

        public DateTimeOffset StartDateTime { get; set; }
        public DateTimeOffset EndDateTime { get; set; }

        public DateTimeOffset PublishedDate { get; set; }
        public DateTimeOffset PreSaleStartDate { get; set; }
        public DateTimeOffset PreSaleEndDate { get; set; }
        public DateTimeOffset OnSaleDate { get; set; }
        public DateTimeOffset OffSaleDate { get; set; }
        public DateTimeOffset GateOpenDate { get; set; }

        public GameCategory? GameCategory { get; set; }
        public ScheduleStatus Status { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public Guid UpdatedBy { get; set; }
        public DateTimeOffset? DeletedAt { get; set; }

        public string ExternalEventKey { get; set; } = null!;

        public IList<EventSection> Sections { get; set; } = [];
        public IList<Ticket> Tickets { get; set; } = [];
        public IList<InventoryBatch> InventoryBatches { get; set; } = [];
        public IList<PriceRule> PriceRules { get; set; } = [];
    }
}
