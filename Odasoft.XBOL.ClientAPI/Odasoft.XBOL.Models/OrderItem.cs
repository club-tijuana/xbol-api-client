using Odasoft.XBOL.Commons.Enums;

namespace Odasoft.XBOL.Models
{
    public class OrderItem : BaseModel
    {
        public long OrderId { get; set; }
        public Order Order { get; set; } = null!;

        public ItemType ItemType { get; set; }
        public long ItemReferenceId { get; set; }

        public bool IsCourtesy { get; set; }
        public decimal Price { get; set; }
    }
}
