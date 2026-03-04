namespace Odasoft.XBOL.DTO
{
    public class EventDetailDTO
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? ShortDescription { get; set; }
        public string? LongDescription { get; set; }
        public string Image { get; set; } = string.Empty;
        public List<string> Gallery { get; set; } = new List<string>();
        public List<EventScheduleDTO> Schedules { get; set; } = new List<EventScheduleDTO>();
        public IList<EventCategoryDTO> Categories { get; set; } = new List<EventCategoryDTO>();
    }
}
