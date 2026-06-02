using Odasoft.XBOL.Commons.Enums;

namespace Odasoft.XBOL.Models
{
    public class ClientLoginIdentifier : BaseModel
    {
        public long ClientId { get; set; }
        public Client Client { get; set; } = null!;
        public ClientLoginIdentifierType Type { get; set; }
        public string NormalizedValue { get; set; } = string.Empty;
        public DateTimeOffset VerifiedAt { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public Guid UpdatedBy { get; set; }
    }
}
