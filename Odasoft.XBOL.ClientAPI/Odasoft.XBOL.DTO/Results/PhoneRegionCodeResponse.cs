using System.ComponentModel;

namespace Odasoft.XBOL.DTO.Results
{
    public class PhoneRegionCodeResponse
    {
        public long Id { get; set; }
        [Description("Region code like \"US\", \"GB\", etc.")]
        public string RegionCode { get; set; } = "";

        [Description("Dial code like \"+1\", \"+44\", etc.")]
        public string DialCode { get; set; } = "";

        public string FlagEmoji { get; set; } = "";
    }
}
