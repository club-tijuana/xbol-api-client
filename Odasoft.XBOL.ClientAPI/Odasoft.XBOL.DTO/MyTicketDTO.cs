namespace Odasoft.XBOL.DTO
{
    public class MyTicketDTO
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTimeOffset StartDate { get; set; }
        public string Location { get; set; } = string.Empty;
        public string EventImage { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Section { get; set; } = string.Empty;
        public string Row { get; set; } = string.Empty;
        public string Seat { get; set; } = string.Empty;
    }
}
