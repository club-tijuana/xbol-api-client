namespace Odasoft.XBOL.Models
{
    public class Gate : BaseModel
    {
        public string Name { get; set; } = null!;

        public long VenueMapId { get; set; }
        public VenueMap VenueMap { get; set; } = null!;

        public IList<TicketScanLog> TicketScanLogs { get; set; } = [];
    }
}
