namespace Odasoft.XBOL.Business.Messages
{
    public record CreateEventBookingCommand(EventBookingRequest Request, long? VerifiedClientId = null);
}
