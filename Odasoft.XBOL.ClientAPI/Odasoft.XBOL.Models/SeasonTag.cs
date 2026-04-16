namespace Odasoft.XBOL.Models
{
    public class SeasonTag : BaseModel
    {
        public long SeasonId { get; set; }
        public Season Season { get; set; } = null!;

        public long TagId { get; set; }
        public Tag Tag { get; set; } = null!;
    }
}
