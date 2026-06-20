namespace Odasoft.XBOL.DTO
{
    public class ZoneDTO
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public decimal? Price { get; set; }
        public long? PriceListItemId { get; set; }
        public List<OrderFeeDTO> Fees { get; set; } = new List<OrderFeeDTO>();
    }
}
