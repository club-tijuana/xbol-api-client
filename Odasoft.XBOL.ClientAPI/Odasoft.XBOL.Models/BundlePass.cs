using Odasoft.XBOL.Commons.Enums;

namespace Odasoft.XBOL.Models
{
    public class BundlePass : BaseModel
    {
        public long BundleId { get; set; }
        public Bundle Bundle { get; set; } = null!;

        public long? ClientId { get; set; }
        public Client? Client { get; set; }

        public Guid? UserId { get; set; }
        public User? User { get; set; }

        public long? BundleSeatId { get; set; }
        public BundleSeat? BundleSeat { get; set; }

        public string TrackingCode { get; set; } = null!;
        public string PrivateToken { get; set; } = null!;

        public BundlePassType BundlePassType { get; set; }
        public BundlePassStatus Status { get; set; }
        public BundlePassSuspendedReason? SuspendedReason { get; set; }
        public string? SuspendedOtherReason { get; set; }

        public bool IsDigital { get; set; } = true;
        public decimal Price { get; set; }

        public DateTimeOffset PurchasedAt { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public Guid UpdatedBy { get; set; }

        public IList<BundlePassEventTicket> BundlePassEventTickets { get; set; } = [];
    }
}
