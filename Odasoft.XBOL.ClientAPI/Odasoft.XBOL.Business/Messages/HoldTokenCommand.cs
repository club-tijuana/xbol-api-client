namespace Odasoft.XBOL.Business.Messages
{
    public record ReleaseSeatsActionCommand(ReleaseSeatsByKeyRequest Request);
    public record HoldSeatsActionCommand(HoldSeatsActionRequest Request);
}
