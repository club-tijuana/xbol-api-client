using Odasoft.XBOL.Commons.Enums;

namespace Odasoft.XBOL.DTO
{
    public class MyTicketDTO
    {
        public long Id { get; set; }
        public string OrderReference { get; set; } = string.Empty;
        public OrderType OrderType { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTimeOffset StartDate { get; set; }
        public string Location { get; set; } = string.Empty;
        public string EventImage { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public TicketType Type { get; set; }
        public decimal PricePaid { get; set; }
        public decimal AdditionalCharges { get; set; }
        public string Section { get; set; } = string.Empty;
        public string Row { get; set; } = string.Empty;
        public string Seat { get; set; } = string.Empty;
        public string MainGate { get; set; } = string.Empty;
        public string QR { get; set; } = string.Empty;
        public bool CanShare { get; set; }
        public bool IsShared { get; set; }
        public bool IsOwner { get; set; }
    }
}
