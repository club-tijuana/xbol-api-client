namespace Odasoft.XBOL.Models
{
    public class BundleEventSchedule
    {
        public long BundleId { get; set; }
        public Bundle Bundle { get; set; } = null!;
        public long EventScheduleId { get; set; }
        public EventSchedule EventSchedule { get; set; } = null!;
        public int? SortOrder { get; set; }
    }
}
