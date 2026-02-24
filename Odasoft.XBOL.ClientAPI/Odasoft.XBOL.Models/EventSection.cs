namespace Odasoft.XBOL.Models
{
    public class EventSection : BaseModel
    {
        public long EventScheduleId { get; set; }

        public EventSchedule EventSchedule { get; set; } = null!;

        public long BaseSectionId { get; set; }
        public BaseSection BaseSection { get; set; } = null!;

        public string DisplayName { get; set; } = null!;

        public decimal? Price { get; set; }

        public int TotalSeats { get; set; }

        public int AvailableSeats { get; set; }

        public IList<Ticket> Tickets { get; set; } = [];
        public IList<EventSeat> EventSeats { get; set; } = [];
        public IList<PriceRule> PriceRules { get; set; } = [];
    }
}
