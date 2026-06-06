namespace Odasoft.XBOL.Models
{
    public class SeasonSection : BaseModel
    {
        public long SeasonId { get; set; }
        public Season Season { get; set; } = null!;

        public long BaseSectionId { get; set; }
        public BaseSection BaseSection { get; set; } = null!;

        public string DisplayName { get; set; } = null!;

        public int TotalSeats { get; set; }
        public int AvailableSeats { get; set; }

        public IList<SeasonSeat> SeasonSeats { get; set; } = [];
    }
}
