namespace Odasoft.XBOL.DTO
{
    public class ScheduleItemDTO
    {
        public long Id { get; set; }
        public required EventItemDTO Event { get; set; }
        public DateTimeOffset StartDate { get; set; }
    }
}
