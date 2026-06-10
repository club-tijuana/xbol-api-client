using Odasoft.XBOL.Commons.Enums;

namespace Odasoft.XBOL.DTO
{
    public class EventCatalogItemDTO
    {
        public required long Id { get; set; }
        public required EventCatalogItemType ItemType { get; set; }
        public BundleType? BundleType { get; set; }
        public required EventStatus Status { get; set; }
        public required DateTimeOffset ScheduledStartDate { get; set; }
        public required string Name { get; set; }
        public string? Code { get; set; }
        public List<EventCategoryDTO> Categories { get; set; } = [];
        public long? VenueMapId { get; set; }
        public string? VenueName { get; set; }
        public string? ExternalEventKey { get; set; }
        public required int AvailableSeats { get; set; }
        public required int TotalSeats { get; set; }
        public EventMediaSetResponse? Media { get; set; }
        public string? PosterImageUrl { get; set; }
        public string? BannerImageUrl { get; set; }
        public bool IsSeason { get; set; }
    }
}
