namespace Odasoft.XBOL.Business
{
    public partial class ZoneResponse
    {
        [Newtonsoft.Json.JsonProperty("fees", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public System.Collections.Generic.ICollection<FeeItemResponse>? Fees { get; set; }
    }

    public partial class SeatResponse
    {
        [Newtonsoft.Json.JsonProperty("fees", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public System.Collections.Generic.ICollection<FeeItemResponse>? Fees { get; set; }
    }

    public class FeeItemResponse
    {
        [Newtonsoft.Json.JsonProperty("feeName", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public string? FeeName { get; set; }

        [Newtonsoft.Json.JsonProperty("feeAmount", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public decimal FeeAmount { get; set; }

        [Newtonsoft.Json.JsonProperty("chargeCategory", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public string? ChargeCategory { get; set; }
    }
}
