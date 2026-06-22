namespace Odasoft.XBOL.DTO
{
    public class InitiatePaymentLinkCheckoutRequest
    {
        public string ReturnUrl { get; set; } = "";
        public string Currency { get; set; } = "MXN";
    }
}
