namespace Odasoft.XBOL.DTO
{
    public class OrderTotalsDTO
    {
        public decimal SubTotal { get; set; }
        public decimal TotalFees { get; set; }
        public decimal TotalTaxes { get; set; }
        public decimal Discount { get; set; }
        public decimal Total { get; set; }
    }
}
