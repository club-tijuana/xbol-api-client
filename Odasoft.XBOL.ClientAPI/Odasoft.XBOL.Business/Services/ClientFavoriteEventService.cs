using Microsoft.EntityFrameworkCore;
using Odasoft.XBOL.Commons.Enums;
using Odasoft.XBOL.Commons.Requests;
using Odasoft.XBOL.Commons.Responses;
using Odasoft.XBOL.Data.Repositories;
using Odasoft.XBOL.DTO;
using Odasoft.XBOL.Models;

namespace Odasoft.XBOL.Business.Services
{
    public class ClientFavoriteEventService
    {
        private readonly ClientFavoriteEventRepository _clientFavoriteEventRepository;


        public ClientFavoriteEventService(
            ClientFavoriteEventRepository clientFavoriteEventRepository)
        {
            _clientFavoriteEventRepository = clientFavoriteEventRepository;
        }

        public async Task<bool> Toggle(long clientId, long eventId)
        {
            var favorite = await _clientFavoriteEventRepository
                               .GetTracked(x => x.ClientId == clientId && x.EventId == eventId)
                               .FirstOrDefaultAsync();

            if (favorite != null)
            {
                _clientFavoriteEventRepository.HardDelete(favorite);
                await _clientFavoriteEventRepository.CommitAsync();
                return false;
            }

            await _clientFavoriteEventRepository.InsertAsync(new ClientFavoriteEvent
            {
                ClientId = clientId,
                EventId = eventId,
                CreatedAt = DateTimeOffset.UtcNow
            });

            await _clientFavoriteEventRepository.CommitAsync();
            return true;
        }
    }
}
