namespace Odasoft.XBOL.DTO
{
    public class SeatDTO
    {
        public long Id { get; set; }
        public string ExternalSeatObjectKey { get; set; } = string.Empty;
        public decimal? PriceOverride { get; set; }
        public long? PriceListItemId { get; set; }
        public List<OrderFeeDTO> Fees { get; set; } = new List<OrderFeeDTO>();
    }
}
