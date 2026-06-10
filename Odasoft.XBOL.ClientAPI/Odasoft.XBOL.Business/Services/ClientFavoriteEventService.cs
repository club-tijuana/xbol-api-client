using Microsoft.EntityFrameworkCore;
using Odasoft.XBOL.Commons.Responses;
using Odasoft.XBOL.Data.Repositories;
using Odasoft.XBOL.DTO;
using Odasoft.XBOL.Models;

namespace Odasoft.XBOL.Business.Services
{
    public class ClientFavoriteEventService
    {
        private readonly ClientFavoriteEventRepository _clientFavoriteEventRepository;

        private const int MIN_PAGE = 1;
        private const int MAX_PAGE = 50;
        private const int MAIN_PAGE_SIZE = 2;
        private const int MAIN_CURRENT_PAGE = 1;

        public ClientFavoriteEventService(
            ClientFavoriteEventRepository clientFavoriteEventRepository)
        {
            _clientFavoriteEventRepository = clientFavoriteEventRepository;
        }

        public async Task<ToggleFavoriteResponse> ToggleAsync(long clientId, long eventId)
        {
            ToggleFavoriteResponse response = new ToggleFavoriteResponse();

            var favorite = await _clientFavoriteEventRepository
                               .GetTracked(x => x.ClientId == clientId && x.EventId == eventId)
                               .FirstOrDefaultAsync();

            if (favorite != null)
            {
                _clientFavoriteEventRepository.HardDelete(favorite);
                await _clientFavoriteEventRepository.CommitAsync();

                response.IsFavorite = false;
                response.Message = "Evento eliminado de tus favoritos";

                return response;
            }

            await _clientFavoriteEventRepository.InsertAsync(new ClientFavoriteEvent
            {
                ClientId = clientId,
                EventId = eventId,
                CreatedAt = DateTimeOffset.UtcNow
            });

            await _clientFavoriteEventRepository.CommitAsync();

            response.IsFavorite = true;
            response.Message = "Evento guardado en tus favoritos";

            return response;
        }

        public async Task<SyncFavoritesResponse> SyncFavoritesAsync(long clientId, List<long> eventIds)
        {
            SyncFavoritesResponse response = new SyncFavoritesResponse
            {
                TotalReceived = eventIds?.Count ?? 0
            };

            if (eventIds == null || !eventIds.Any())
            {
                return response;
            }

            eventIds = eventIds.Distinct().ToList();

            var existingEventIds = await _clientFavoriteEventRepository
                .Get(x => x.ClientId == clientId && eventIds.Contains(x.EventId))
                .Select(x => x.EventId)
                .ToListAsync();

            var newEventIds = eventIds.Except(existingEventIds).ToList();

            response.AlreadyExists = existingEventIds.Count;
            response.Inserted = newEventIds.Count;

            if (!newEventIds.Any())
            {
                return response;
            }

            var newFavorites = newEventIds.Select(eventId => new ClientFavoriteEvent
            {
                ClientId = clientId,
                EventId = eventId,
                CreatedAt = DateTimeOffset.UtcNow
            }).ToList();

            await _clientFavoriteEventRepository.InsertRangeAsync(newFavorites);
            await _clientFavoriteEventRepository.CommitAsync();

            return response;
        }
        public async Task<PagedResponse<EventItemDTO>> GetFavoritesByClientIdAsync(int? page, int? pageSize, long clientId, bool includeMedia = false)
        {
            return await _clientFavoriteEventRepository.GetFavoritesByClientIdAsync(page ?? MIN_PAGE, pageSize ?? MAX_PAGE, clientId, includeMedia);
        }

        public async Task<List<long>> GetFavoritesIdsByClientIdAsync(long clientId)
        {
            var eventIds = await _clientFavoriteEventRepository.Get(
                    filter: f => f.ClientId == clientId
                )
                .Select(f => f.EventId)
                .ToListAsync();

            return eventIds;
        }
    }
}
