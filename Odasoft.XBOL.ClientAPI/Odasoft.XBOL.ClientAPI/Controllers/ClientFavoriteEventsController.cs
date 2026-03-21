using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Odasoft.XBOL.Business.Services;

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
        public async Task<IActionResult> Toggle([FromRoute] long eventId)
        {
            // TODO: Remove temp token
            var authHeader = Request.Headers["Authorization"].FirstOrDefault();

            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                return Unauthorized("Token no válido");
            }

            var token = authHeader.Replace("Bearer ", "").Trim();

            long clientId = token switch
            {
                "TEST-TOKEN" => 1,
                _ => 2
            };

            bool isFavorite = await _clientFavoriteEventService.Toggle(clientId, eventId);

            return Ok(new
            {
                isFavorite,
                message = isFavorite
                    ? "Evento guardado en tus favoritos"
                    : "Evento eliminado de tus favoritos"
            });
        }
    }
}
