namespace Odasoft.XBOL.DTO
{
    public class MyEventTicketDTO
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTimeOffset StartDate { get; set; }
        public string Location { get; set; } = string.Empty;
        public bool isSeasonPass { get; set; }
        public bool isPastEvent { get; set; }
    }
}
