using Odasoft.XBOL.Commons.Enums;

namespace Odasoft.XBOL.Models
{
    public class Client : BaseModel
    {
        public ClientType ClientType { get; set; }
        public string? FullName { get; set; }
        public DateTimeOffset? DateOfBirth { get; set; }
        public Gender? Gender { get; set; }
        public string? BusinessName { get; set; }
        public string? Email { get; set; }
        public long? PhoneRegionCodeId { get; set; }
        public PhoneRegionCode? PhoneRegionCode { get; set; }
        public string? PhoneNumber { get; set; }
        public string? TaxId { get; set; }
        public string? Country { get; set; }
        public string? State { get; set; }
        public string? City { get; set; }
        public string? StreetAddress { get; set; }
        public string? ExtNum { get; set; }
        public string? IntNum { get; set; }
        public string? PostalCode { get; set; }
        public string? Neighborhood { get; set; }
        public bool IsActive { get; set; }

        public string? FirebaseUid { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public Guid UpdatedBy { get; set; }
        public IList<Order> Orders { get; set; } = [];
        public ClientCreditAccount? ClientCreditAccount { get; set; }
        public IList<Ticket> Tickets { get; set; } = [];
        public IList<ClientFavoriteEvent> FavoriteEvents { get; set; } = [];
        public IList<ClientLoginIdentifier> LoginIdentifiers { get; set; } = [];
        public LegalRepresentative? LegalRepresentative { get; set; }
    }
}
