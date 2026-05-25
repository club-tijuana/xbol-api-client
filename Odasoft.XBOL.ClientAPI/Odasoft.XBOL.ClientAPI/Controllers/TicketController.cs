using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Odasoft.XBOL.Business.Exceptions;
using Odasoft.XBOL.Business.Services;
using Odasoft.XBOL.ClientAPI.Services;
using Odasoft.XBOL.Commons.Requests;
using Odasoft.XBOL.DTO;

namespace Odasoft.XBOL.ClientAPI.Controllers
{
    [Route("api/tickets")]
    [ApiController]
    public class TicketController : ControllerBase
    {
        private readonly TicketService _ticketService;
        private readonly ILogger<TicketController> _logger;
        private readonly IClientIdentityService _clientIdentityService;

        public TicketController(
            TicketService ticketService,
            ILogger<TicketController> logger,
            IClientIdentityService clientIdentityService)
        {
            _ticketService = ticketService;
            _logger = logger;
            _clientIdentityService = clientIdentityService;
        }

        [HttpPost("share")]
        [Authorize]
        [EndpointName("ShareTicketAsync")]
        public async Task<ActionResult<MyTicketDTO>> ShareTicketAsync([FromBody] ShareTicketRequest request)
        {
            var client = await _clientIdentityService.RequireCurrentClientAsync(User);

            try
            {
                var result = await _ticketService.ShareTicketAsync(request, client.Id);

                if (result == null)
                {
                    return NotFound();
                }

                return Ok(result);
            }
            catch (ClientNotFoundException)
            {
                return NotFound(new { message = "Client not found" });
            }
            catch (TicketNotFoundException)
            {
                return NotFound(new { message = "Ticket not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to share ticket");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("unshare")]
        [Authorize]
        [EndpointName("UnshareTicketAsync")]
        public async Task<ActionResult<MyTicketDTO>> UnshareTicketAsync([FromBody] UnshareTicketRequest request)
        {
            var client = await _clientIdentityService.RequireCurrentClientAsync(User);

            try
            {
                var result = await _ticketService.UnshareTicketAsync(request, client.Id);

                return Ok(result);
            }
            catch (TicketNotFoundException)
            {
                return NotFound(new { message = "Ticket not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to unshare ticket");
                return BadRequest(ex.Message);
            }
        }
    }
}
