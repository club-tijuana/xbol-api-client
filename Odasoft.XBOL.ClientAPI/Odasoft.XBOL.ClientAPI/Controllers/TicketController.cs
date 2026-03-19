using Microsoft.AspNetCore.Mvc;
using Odasoft.XBOL.Business.Exceptions;
using Odasoft.XBOL.Business.Services;
using Odasoft.XBOL.Commons.Requests;
using Odasoft.XBOL.DTO;

namespace Odasoft.XBOL.ClientAPI.Controllers
{
    [Route("api/tickets")]
    [ApiController]
    public class TicketController : ControllerBase
    {
        private readonly TicketService _ticketService;

        public TicketController(TicketService ticketService)
        {
            _ticketService = ticketService;
        }

        [HttpPost("share")]
        [EndpointName("ShareTicketAsync")]
        public async Task<ActionResult<MyTicketDTO>> ShareTicketAsync([FromBody] ShareTicketRequest request)
        {
            try
            {
                // TODO: Remove temp token
                var authHeader = Request.Headers["Authorization"].ToString();
                if (authHeader.StartsWith("Bearer "))
                {
                    var token = authHeader.Substring("Bearer ".Length);
                    long idClient = token == "TEST-TOKEN" ? 1 : 2;

                    var result = await _ticketService.ShareTicketAsync(request, idClient);

                    if (result == null)
                    {
                        return NotFound();
                    }

                    return Ok(result);
                }
                else
                {
                    return Unauthorized();
                }
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
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("unshare")]
        [EndpointName("UnshareTicketAsync")]
        public async Task<ActionResult<MyTicketDTO>> UnshareTicketAsync([FromBody] UnshareTicketRequest request)
        {
            try
            {
                // TODO: Remove temp token
                var authHeader = Request.Headers["Authorization"].ToString();
                if (authHeader.StartsWith("Bearer "))
                {
                    var token = authHeader.Substring("Bearer ".Length);
                    long idClient = token == "TEST-TOKEN" ? 1 : 2;

                    var result = await _ticketService.UnshareTicketAsync(request, idClient);

                    return Ok(result);
                }
                else
                {
                    return Unauthorized();
                }
            }
            catch (TicketNotFoundException)
            {
                return NotFound(new { message = "Ticket not found" });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
