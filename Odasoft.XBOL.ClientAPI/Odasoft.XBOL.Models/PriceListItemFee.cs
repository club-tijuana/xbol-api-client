namespace Odasoft.XBOL.Models
{
    public class PriceListItemFee : BaseModel
    {
        public long PriceListItemId { get; set; }
        public string FeeType { get; set; } = null!;
        public string ChargeCategory { get; set; } = "Fee";
        public decimal FeeAmount { get; set; }
    }
}
