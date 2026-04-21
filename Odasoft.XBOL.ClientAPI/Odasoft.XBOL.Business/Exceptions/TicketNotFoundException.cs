namespace Odasoft.XBOL.Business.Exceptions
{
    public class TicketNotFoundException : Exception
    {
        public TicketNotFoundException() : base("Ticket not found") { }
    }
}
