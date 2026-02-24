using Odasoft.XBOL.Commons.Enums;

namespace Odasoft.XBOL.Models
{
    public class BaseSeat : BaseModel
    {
        public long BaseRowId { get; set; }
        public BaseRow BaseRow { get; set; } = null!;

        public string SeatNumber { get; set; } = null!;
        public SeatType SeatType { get; set; }

        public IList<EventSeat> EventSeats { get; set; } = [];
    }
}
