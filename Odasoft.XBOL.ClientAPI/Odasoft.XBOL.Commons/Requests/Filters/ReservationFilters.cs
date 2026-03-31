namespace Odasoft.XBOL.Commons.Requests.Filters
{
    public class ReservationFilters
    {
        public long? SeasonId { get; set; }
        public long? ScheduleId { get; set; }
        public long? SectionId { get; set; }
        public long? ZoneId { get; set; }
        public PriceRange? PriceRange { get; set; }
    }

    public class PriceRange
    {
        public decimal? Min { get; set; }
        public decimal? Max { get; set; }
    }
}
