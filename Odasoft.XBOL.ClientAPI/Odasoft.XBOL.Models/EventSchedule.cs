using Odasoft.XBOL.Commons.Enums;

namespace Odasoft.XBOL.Models
{
    public class EventSchedule : BaseModel
    {
        public long EventId { get; set; }

        public DateTimeOffset StartDateTime { get; set; }

        public DateTimeOffset EndDateTime { get; set; }

        public Event Event { get; set; } = null!;
        public DateTimeOffset PublishedDate { get; set; }

        public DateTimeOffset OnSaleDate { get; set; }

        public DateTimeOffset PreSaleDate { get; set; }

        public DateTimeOffset OffSaleDate { get; set; }

        public DateTimeOffset GateOpenDate { get; set; }

        public GameCategory GameCategory { get; set; }

        public ScheduleStatus Status { get; set; }

        public string ExternalEventKey { get; set; } = null!;

        public IList<EventSection> Sections { get; set; } = [];
        public IList<Ticket> Tickets { get; set; } = [];
        public IList<PriceRule> PriceRules { get; set; } = [];
    }
}
