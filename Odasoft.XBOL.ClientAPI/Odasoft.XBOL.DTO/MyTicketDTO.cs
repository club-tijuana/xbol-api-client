namespace Odasoft.XBOL.DTO
{
    public class MyTicketDTO
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTimeOffset StartDate { get; set; }
        public string Location { get; set; } = string.Empty;
    }
}
