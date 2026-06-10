namespace Odasoft.XBOL.DTO
{
    public class CanRenewOrderResponse
    {
        public long? OrderId { get; set; }
        public bool CanRenew { get; set; }
        public string? Reference { get; set; }
        public long? NewSeasonId { get; set; }

        public long? RenewableSeats { get; set; }
        public long? TotalSeats { get; set; }
    }
}
