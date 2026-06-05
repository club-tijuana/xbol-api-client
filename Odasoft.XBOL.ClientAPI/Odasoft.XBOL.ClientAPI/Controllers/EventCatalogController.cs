using Microsoft.AspNetCore.Mvc;
using Odasoft.XBOL.Business.Services;
using Odasoft.XBOL.Commons.Requests;
using Odasoft.XBOL.Commons.Responses;
using Odasoft.XBOL.DTO;

namespace Odasoft.XBOL.ClientAPI.Controllers
{
    [Route("api/event-catalog")]
    [ApiController]
    public class EventCatalogController(EventCatalogService eventCatalogService) : ControllerBase
    {
        [HttpGet]
        [EndpointName("GetEventCatalogItemsAsync")]
        [ProducesResponseType(typeof(PagedResponse<EventCatalogItemDTO>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PagedResponse<EventCatalogItemDTO>>> GetItemsAsync(
            [FromQuery] EventCatalogQueryParams queryParams)
        {
            var result = await eventCatalogService.GetItemsAsync(queryParams);

            return Ok(result);
        }

        [HttpGet("{id}")]
        [EndpointName("GetEventCatalogItemAsync")]
        [ProducesResponseType(typeof(EventCatalogItemDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<EventCatalogItemDTO>> GetItemAsync(
            [FromRoute] long id,
            // TODO: Consolidate DTOs and enums usage.
            // Currently, some services use the locally defined models while others use the OpenAPI generated models.
            // Mappings were added as a temporary workaround, but the project should standarize on a single source of truth.
            [FromQuery] Business.EventCatalogItemType? itemType = null)
        {
            var result = await eventCatalogService.GetItemAsync(id, itemType);

            return result is null
                ? NotFound()
                : Ok(result);
        }
    }
}
