namespace Odasoft.XBOL.Business.Exceptions
{
    public class PaymentLinkException : Exception
    {
        public PaymentLinkException(string message) : base(message)
        {

        }
    }

    public class PaymentLinkExpiredException : PaymentLinkException
    {
        public PaymentLinkExpiredException()
            : base("Payment link expired.")
        {
        }
    }

    public class PaymentLinkAlreadyUsedException : PaymentLinkException
    {
        public PaymentLinkAlreadyUsedException()
            : base("Payment link already used.")
        {
        }
    }

    public class PaymentLinkCanceledException : PaymentLinkException
    {
        public PaymentLinkCanceledException()
            : base("Payment link is canceled.")
        {
        }
    }
}
