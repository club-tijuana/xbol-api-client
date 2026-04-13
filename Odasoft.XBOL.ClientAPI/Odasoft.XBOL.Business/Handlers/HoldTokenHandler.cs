using Odasoft.XBOL.Business.Messages;

namespace Odasoft.XBOL.Business.Handlers
{
    public class HoldTokenHandler
    {
        private readonly ITicketingClient _ticketingClient;

        public HoldTokenHandler(ITicketingClient ticketingClient)
        {
            _ticketingClient = ticketingClient;
        }

        public async Task<HoldToken> Handle(HoldTokenCommand _)
        {
            return await _ticketingClient.ClientHoldSeatsAsync();
        }

        public async Task<HoldToken> Handle(ReleaseHoldSeatsCommand message)
        {
            HoldToken holdToken = await _ticketingClient.ReleaseHoldSeatsAsync(message.Request);
            return holdToken;
        }
    }
}
