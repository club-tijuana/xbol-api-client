using Odasoft.XBOL.Commons.Enums;

namespace Odasoft.XBOL.Models
{
    public class Venue : BaseModel
    {
        public string Name { get; set; } = string.Empty;
        public string AddressLine { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string? ZipCode { get; set; }

        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }

        public VenueCategory Category { get; set; }

        public string ShortDescription { get; set; } = string.Empty;
        public string LongDescription { get; set; } = string.Empty;

        public string LogoImageUrl { get; set; } = string.Empty;
        public string BannerImageUrl { get; set; } = string.Empty;
        public string LandingUrl { get; set; } = string.Empty;

        public string? ContactEmail { get; set; }
        public long? PhoneRegionCodeId { get; set; }
        public PhoneRegionCode? PhoneRegionCode { get; set; }
        public string? ContactPhoneNumber { get; set; }

        public bool IsActive { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public Guid UpdatedBy { get; set; }

        public IList<VenueMap> VenueMaps { get; set; } = [];
    }
}
