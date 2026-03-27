using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Odasoft.XBOL.Business.Services;
using Odasoft.XBOL.Commons.Responses;
using Odasoft.XBOL.DTO;

namespace Odasoft.XBOL.ClientAPI.Controllers
{
    [Route("api/favorites")]
    [ApiController]
    public class ClientFavoriteEventsController : Controller
    {
        private readonly ClientFavoriteEventService _clientFavoriteEventService;

        public ClientFavoriteEventsController(ClientFavoriteEventService clientFavoriteEventService)
        {
            _clientFavoriteEventService = clientFavoriteEventService;
        }

        [HttpPost("{eventId}/toggle")]
        [EndpointName("ToggleFavoriteAsync")]
        [ProducesResponseType(typeof(ToggleFavoriteResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<ToggleFavoriteResponse>> ToggleFavoriteAsync([FromRoute] long eventId)
        {

            // TODO: Remove temp clientId
            long clientId = 1;

            ToggleFavoriteResponse response = await _clientFavoriteEventService.ToggleAsync(clientId, eventId);

            return Ok(response);
        }

        [HttpPost("sync")]
        [EndpointName("SyncFavoritesAsync")]
        [ProducesResponseType(typeof(SyncFavoritesResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<SyncFavoritesResponse>> SyncFavoritesAsync([FromBody] List<long> eventIds)
        {
            // TODO: Remove temp clientId
            long clientId = 1;

            SyncFavoritesResponse response = await _clientFavoriteEventService.SyncFavoritesAsync(clientId, eventIds);

            return Ok(response);
        }

        [HttpGet("trending-events")]
        [EndpointName("GetTrendingEventsAsync")]
        [ProducesResponseType(typeof(PagedResponse<EventItemDTO>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PagedResponse<EventItemDTO>>> GetFavoritesByClientIdAsync(
            [FromQuery] int? page,
            [FromQuery] int? pageSize)
        {
            // TODO: Remove temp token
            long? clientId = null;

            var authHeader = Request.Headers["Authorization"].ToString();
            if (authHeader.StartsWith("Bearer "))
            {
                var token = authHeader.Substring("Bearer ".Length);
                clientId = token == "TEST-TOKEN" ? 1 : 2;

            }
            else
            {
                clientId = 1;
            }

            var result = await _clientFavoriteEventService.GetFavoritesByClientIdAsync(page, pageSize, clientId.Value);

            return Ok(result);
        }

    }
}
