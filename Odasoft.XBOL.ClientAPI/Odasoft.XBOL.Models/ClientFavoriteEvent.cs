using Odasoft.XBOL.Commons.Enums;

namespace Odasoft.XBOL.Models
{
    public class ClientFavoriteEvent : BaseModel
    {
        public long ClientId { get; set; }
        public Client Client { get; set; } = null!;
        public long EventId { get; set; }
        public Event Event { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
