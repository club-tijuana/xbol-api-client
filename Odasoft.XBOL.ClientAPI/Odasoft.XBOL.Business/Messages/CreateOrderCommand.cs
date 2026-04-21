namespace Odasoft.XBOL.Business.Messages
{
    public record CreateOrderCommand(EventBookingRequest Request);
    public record CreateSeasonOrderCommand(SeasonBookingRequest Request);
}
