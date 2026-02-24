namespace Odasoft.XBOL.Models
{
    public class OrderFee : BaseModel
    {
        public long OrderId { get; set; }
        public Order Order { get; set; } = null!;

        public string FeeType { get; set; } = null!;
        public decimal Amount { get; set; }
    }
}
