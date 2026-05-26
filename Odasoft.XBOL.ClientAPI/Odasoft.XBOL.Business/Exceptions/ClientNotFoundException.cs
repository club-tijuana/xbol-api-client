namespace Odasoft.XBOL.Business.Exceptions
{
    public class ClientNotFoundException : Exception
    {
        public ClientNotFoundException() : base("Client not found") { }
    }
}
