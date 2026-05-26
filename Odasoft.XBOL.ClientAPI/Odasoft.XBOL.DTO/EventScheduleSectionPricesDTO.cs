namespace Odasoft.XBOL.DTO
{
    public class EventScheduleSectionPricesDTO
    {
        public List<string> Objects { get; set; } = new List<string>();
        public decimal? Price { get; set; }
        public string Currency { get; set; } = string.Empty;
    }
}
