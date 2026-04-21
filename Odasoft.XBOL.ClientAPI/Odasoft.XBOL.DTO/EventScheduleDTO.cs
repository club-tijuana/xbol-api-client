namespace Odasoft.XBOL.DTO
{
    public class EventScheduleDTO
    {
        public long Id { get; set; }
        public DateTimeOffset Date { get; set; }
        public string Location { get; set; } = string.Empty;
        public List<EventScheduleSectionPricesDTO> SectionPrices { get; set; } = new List<EventScheduleSectionPricesDTO>();
    }
}
