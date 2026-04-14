using Odasoft.XBOL.Commons.Enums;

namespace Odasoft.XBOL.Models
{
    public class SeasonPass : BaseModel
    {
        public long SeasonId { get; set; }
        public Season Season { get; set; } = null!;

        public long? ClientId { get; set; }
        public Client? Client { get; set; }

        public Guid? UserId { get; set; }
        public User? User { get; set; }

        public long? SeasonSeatId { get; set; }
        public SeasonSeat? SeasonSeat { get; set; }

        public string TrackingCode { get; set; } = null!;
        public string PrivateToken { get; set; } = null!;

        public SeasonPassType SeasonPassType { get; set; }
        public SeasonPassStatus Status { get; set; }
        public SeasonPassSuspendedReason? SuspendedReason { get; set; } = null!;
        public string? SuspendedOtherReason { get; set; } = null!;

        public bool IsDigital { get; set; } = true;
        public decimal Price { get; set; }

        public DateTimeOffset PurchasedAt { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public Guid UpdatedBy { get; set; }

        public IList<SeasonPassEventTicket> SeasonPassEventTickets { get; set; } = [];
    }
}
