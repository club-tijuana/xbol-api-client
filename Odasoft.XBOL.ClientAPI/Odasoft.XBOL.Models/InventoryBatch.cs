using Odasoft.XBOL.Commons.Enums;

namespace Odasoft.XBOL.Models
{
    public class InventoryBatch : BaseModel
    {
        public long EventScheduleId { get; set; }
        public EventSchedule EventSchedule { get; set; } = null!;

        public int Quantity { get; set; }
        public DateTimeOffset CutoffDate { get; set; }

        public InventoryBatchStatus Status { get; set; }

        public IList<Ticket> Tickets { get; set; } = [];
    }
}
