using Odasoft.XBOL.Commons.Enums;

namespace Odasoft.XBOL.Models
{
    public class Venue : BaseModel
    {
        public string Name { get; set; } = "";
        public VenueCategory Category { get; set; }
        public string Country { get; set; } = "";
        public string State { get; set; } = "";
        public string City { get; set; } = "";
        public string Neighborhood { get; set; } = "";
        public string StreetAddress { get; set; } = "";
        public string ExtNum { get; set; } = "";
        public string IntNum { get; set; } = "";
        public string ZipCode { get; set; } = "";
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public string ShortDescription { get; set; } = "";
        public string LongDescription { get; set; } = "";
        public string LandingUrl { get; set; } = "";
        public string ContactName { get; set; } = "";
        public string ContactEmail { get; set; } = "";
        public long? PhoneRegionCodeId { get; set; }
        public PhoneRegionCode? PhoneRegionCode { get; set; }
        public string ContactPhoneNumber { get; set; } = "";
        public VenueStatus Status { get; set; }
        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public Guid UpdatedBy { get; set; }
        public IList<VenueMap> VenueMaps { get; set; } = [];
        public IList<VenueImage> VenueImages { get; set; } = [];
    }
}
