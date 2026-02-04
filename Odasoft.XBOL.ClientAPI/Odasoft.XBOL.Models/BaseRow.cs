namespace Odasoft.XBOL.Models
{
    public class BaseRow : BaseModel
    {
        public long BaseSectionId { get; set; }
        public BaseSection BaseSection { get; set; } = null!;

        public string RowLabel { get; set; } = null!;

        public IList<BaseSeat> BaseSeats { get; set; } = [];
    }
}
