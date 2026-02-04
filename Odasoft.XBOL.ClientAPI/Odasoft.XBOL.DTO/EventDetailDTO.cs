namespace Odasoft.XBOL.DTO
{
    public class EventDetailDTO
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? ShortDescription { get; set; }
        public string? LongDescription { get; set; }
        public List<string> Gallery { get; set; } = new List<string>();
        public List<EventScheduleDTO> Schedules { get; set; } = new List<EventScheduleDTO>();
    }
}
