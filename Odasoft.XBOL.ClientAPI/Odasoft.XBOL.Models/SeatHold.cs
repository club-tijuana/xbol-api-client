using Odasoft.XBOL.Commons.Enums;

namespace Odasoft.XBOL.Models
{
    public class SeatHold : BaseModel
    {
        public long EventSeatId { get; set; }
        public EventSeat EventSeat { get; set; } = null!;

        public long? ClientId { get; set; }
        public Client? Client { get; set; }

        public Guid? UserId { get; set; }
        public User? User { get; set; }

        public SeatHoldSource HoldSource { get; set; }
        public string ExternalReference { get; set; } = null!;

        public DateTimeOffset ExpiresAt { get; set; }
        public SeatHoldStatus Status { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
    }
}
