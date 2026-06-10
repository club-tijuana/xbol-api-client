namespace Odasoft.XBOL.DTO
{
    public class SeatAvailabilityDTO
    {
        public IList<ZoneDTO> Zones { get; set; } = new List<ZoneDTO>();
        public IList<SeatDTO> SeatOverrides { get; set; } = new List<SeatDTO>();
    }
}
