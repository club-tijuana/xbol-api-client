namespace Odasoft.XBOL.Models
{
    public class LegalRepresentative : BaseModel
    {
        public long ClientId { get; set; }
        public Client Client { get; set; } = default!;
        public string FullName { get; set; } = "";
        public DateTimeOffset DOB { get; set; }
        public string TaxId { get; set; } = "";
        public string CURP { get; set; } = "";
    }
}
