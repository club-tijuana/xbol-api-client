using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Odasoft.XBOL.Business.Services;
using Odasoft.XBOL.ClientAPI.Services;
using Odasoft.XBOL.Commons.Responses;
using Odasoft.XBOL.DTO;

namespace Odasoft.XBOL.ClientAPI.Controllers
{
    [Route("api/favorites")]
    [ApiController]
    public class ClientFavoriteEventsController : Controller
    {
        private readonly ClientFavoriteEventService _clientFavoriteEventService;
        private readonly IClientIdentityService _clientIdentityService;

        public ClientFavoriteEventsController(
            ClientFavoriteEventService clientFavoriteEventService,
            IClientIdentityService clientIdentityService)
        {
            _clientFavoriteEventService = clientFavoriteEventService;
            _clientIdentityService = clientIdentityService;
        }

        [HttpPost("{eventId}/toggle")]
        [Authorize]
        [EndpointName("ToggleFavoriteAsync")]
        [ProducesResponseType(typeof(ToggleFavoriteResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<ToggleFavoriteResponse>> ToggleFavoriteAsync([FromRoute] long eventId)
        {
            var client = await _clientIdentityService.RequireCurrentClientAsync(User);

            ToggleFavoriteResponse response = await _clientFavoriteEventService.ToggleAsync(client.Id, eventId);

            return Ok(response);
        }

        [HttpPost("sync")]
        [Authorize]
        [EndpointName("SyncFavoritesAsync")]
        [ProducesResponseType(typeof(SyncFavoritesResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<SyncFavoritesResponse>> SyncFavoritesAsync([FromBody] List<long> eventIds)
        {
            var client = await _clientIdentityService.RequireCurrentClientAsync(User);

            SyncFavoritesResponse response = await _clientFavoriteEventService.SyncFavoritesAsync(client.Id, eventIds);

            return Ok(response);
        }

        [HttpGet("get-list-by-clientid")]
        [Authorize]
        [EndpointName("GetFavoritesByClientIdAsync")]
        [ProducesResponseType(typeof(PagedResponse<EventItemDTO>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PagedResponse<EventItemDTO>>> GetFavoritesByClientIdAsync(
            [FromQuery] int? page,
            [FromQuery] int? pageSize)
        {
            var client = await _clientIdentityService.RequireCurrentClientAsync(User);

            var result = await _clientFavoriteEventService.GetFavoritesByClientIdAsync(page, pageSize, client.Id);

            return Ok(result);
        }

        [HttpGet("get-client-favorites-ids")]
        [Authorize]
        [EndpointName("GetFavoritesIdsByClientIdAsync")]
        [ProducesResponseType(typeof(List<long>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<long>>> GetFavoritesIdsByClientIdAsync()
        {
            var client = await _clientIdentityService.RequireCurrentClientAsync(User);
            var result = await _clientFavoriteEventService.GetFavoritesIdsByClientIdAsync(client.Id);

            return Ok(result);
        }
    }
}
