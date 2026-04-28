namespace Odasoft.XBOL.Models
{
    public class EventCategory : BaseModel
    {
        public string Name { get; set; } = null!;
        public string DisplayName { get; set; } = null!;
        public bool IsActive { get; set; }
        public IList<Event> Events { get; set; } = new List<Event>();
    }
}
