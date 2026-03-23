namespace Odasoft.XBOL.DTO.Results
{
    public class BookingResult
    {
        public long? BookingId { get; set; }
        public long? OrderId { get; set; }
        public string Message { get; set; } = "";
        public IEnumerable<string> Tickets { get; set; } = [];
    }
}
