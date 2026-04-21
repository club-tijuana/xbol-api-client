namespace Odasoft.XBOL.DTO
{
    public class SeatAvailabilityDTO
    {
        public IList<SectionDTO> Sections { get; set; } = new List<SectionDTO>();
        public IList<SeatDTO> SeatOverrides { get; set; } = new List<SeatDTO>();
    }
}
