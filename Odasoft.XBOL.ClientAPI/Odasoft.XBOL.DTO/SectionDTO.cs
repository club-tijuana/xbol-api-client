namespace Odasoft.XBOL.DTO
{
    public class SectionDTO
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public decimal? Price { get; set; }
    }
}
