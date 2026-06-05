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

        public async Task<ICollection<string>> Handle(ReleaseSeatsActionCommand message)
        {
            var releasedSeats = await _ticketingClient.ReleaseSeatsActionAsync(message.Request);
            return releasedSeats;
        }

        public async Task<HoldToken> Handle(HoldSeatsActionCommand message)
        {
            HoldToken holdToken = await _ticketingClient.HoldSeatsActionAsync(message.Request);
            return holdToken;
        }
    }
}
