using Odasoft.XBOL.Commons.Enums;

namespace Odasoft.XBOL.DTO
{
    public class EventDetailDTO
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? ShortDescription { get; set; }
        public string? LongDescription { get; set; }
        public string Image { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string? ZipCode { get; set; }
        public string FullAddress { get; set; } = string.Empty;
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public AgeRestriction? AgeRestriction { get; set; }
        public string? SecurityPolicies { get; set; }
        public bool IsFavorite { get; set; } = false;
        public List<string> Gallery { get; set; } = new List<string>();
        public List<EventScheduleDTO> Schedules { get; set; } = new List<EventScheduleDTO>();
        public IList<EventCategoryDTO> Categories { get; set; } = new List<EventCategoryDTO>();
        public EventImagesDTO? Images { get; set; }
    }
}
