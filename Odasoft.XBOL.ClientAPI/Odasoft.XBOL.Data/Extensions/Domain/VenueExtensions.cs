using Odasoft.XBOL.Models;

namespace Odasoft.XBOL.Data.Extensions.Domain
{
    public static class VenueExtensions
    {
        public static string GetFullAddress(this Venue venue)
        {
            var parts = new List<string>
            {
                venue.StreetAddress,
                string.IsNullOrWhiteSpace(venue.ExtNum) ? null : $"Ext. {venue.ExtNum}",
                string.IsNullOrWhiteSpace(venue.IntNum) ? null : $"Int. {venue.IntNum}",
                venue.Neighborhood,
                venue.City,
                venue.State,
                venue.ZipCode,
                venue.Country
            };

            return string.Join(", ", parts.Where(x => !string.IsNullOrWhiteSpace(x)));
        }
    }
}
