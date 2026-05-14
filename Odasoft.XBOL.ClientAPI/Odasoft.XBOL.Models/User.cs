using System.ComponentModel.DataAnnotations;

namespace Odasoft.XBOL.Models
{
    public class User
    {
        [Key]
        public Guid Id { get; set; }

        public string? UserName { get; set; }
        public string? NormalizedUserName { get; set; }
        public string? Email { get; set; }
        public string? NormalizedEmail { get; set; }
        public bool EmailConfirmed { get; set; }
        public string? PasswordHash { get; set; }
        public string? SecurityStamp { get; set; }
        public string? ConcurrencyStamp { get; set; }
        public string? PhoneNumber { get; set; }
        public bool PhoneNumberConfirmed { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }
        public bool LockoutEnabled { get; set; }
        public int AccessFailedCount { get; set; }

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

        public string? FirebaseUid { get; set; }
        public DateTimeOffset? RefreshTokensRevokedAt { get; set; }

        public DateTimeOffset? LastLogin { get; set; }

        public IList<Order> Orders { get; set; } = [];
        public IList<PromoCodeRedemption> PromoCodeRedemptions { get; set; } = [];
        public IList<SeatHold> SeatHolds { get; set; } = [];
        public IList<AuditLog> AuditLogs { get; set; } = [];
        public IList<Ticket> Tickets { get; set; } = [];
    }
}
