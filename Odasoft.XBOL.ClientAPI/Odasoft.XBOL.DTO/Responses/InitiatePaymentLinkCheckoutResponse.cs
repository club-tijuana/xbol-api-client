namespace Odasoft.XBOL.DTO
{
    public class InitiatePaymentLinkCheckoutResponse
    {
        public string SessionId { get; set; } = "";
        public string SuccessIndicator { get; set; } = "";
        public string OrderRefId { get; set; } = "";
        public string MerchantId { get; set; } = "";
        public string ApiVersion { get; set; } = "";
        public string GatewayBaseUrl { get; set; } = "";
        public string Amount { get; set; } = "";
        public string Currency { get; set; } = "";
    }
}
