using Microsoft.AspNetCore.Identity;

namespace Odasoft.XBOL.Models
{
    public class User : IdentityUser<Guid>
    {
        public long? ClientId { get; set; }
        public Client? Client { get; set; }

        public long? OrganizerMemberId { get; set; }
        public OrganizerMember? OrganizerMember { get; set; }
        public long? PhoneRegionCodeId { get; set; }
        public PhoneRegionCode? PhoneRegionCode { get; set; }

        public DateTimeOffset? EmailVerifiedTimeStamp { get; set; }
        public DateTimeOffset? PhoneVerifiedTimeStamp { get; set; }

        public bool IsActive { get; set; }
        public bool IsMfaEnabled { get; set; }
        public string? MfaMethod { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }

        public Guid CreatedBy { get; set; }
        public Guid UpdatedBy { get; set; }

        public DateTimeOffset? LastLogin { get; set; }

        public IList<Order> Orders { get; set; } = [];
        public IList<PromoCodeRedemption> PromoCodeRedemptions { get; set; } = [];
        public IList<SeatHold> SeatHolds { get; set; } = [];
        public IList<AuditLog> AuditLogs { get; set; } = [];
        public IList<Ticket> Tickets { get; set; } = [];
    }
}
