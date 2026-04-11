using Odasoft.XBOL.Business.Messages;

namespace Odasoft.XBOL.Business.Handlers
{
    public class ManageSeatsHandler
    {
        private readonly TicketingClient _ticketingClient;

        public ManageSeatsHandler(TicketingClient ticketingClient)
        {
            _ticketingClient = ticketingClient;
        }

        public async Task Handle(ManageSeatsCommand message)
        {
            await _ticketingClient.SetForSaleAsync(message.Request);
        }
    }
}
