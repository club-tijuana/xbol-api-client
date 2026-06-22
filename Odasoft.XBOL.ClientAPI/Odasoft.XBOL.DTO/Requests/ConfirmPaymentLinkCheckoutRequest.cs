namespace Odasoft.XBOL.DTO
{
    public class ConfirmPaymentLinkCheckoutRequest
    {
        public string OrderRefId { get; set; } = "";
        public string ResultIndicator { get; set; } = "";
        public string SuccessIndicator { get; set; } = "";
    }
}
