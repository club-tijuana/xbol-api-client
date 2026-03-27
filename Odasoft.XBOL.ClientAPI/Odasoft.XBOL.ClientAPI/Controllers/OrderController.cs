using Microsoft.AspNetCore.Mvc;
using Odasoft.XBOL.Business.Services;
using Odasoft.XBOL.DTO;

namespace Odasoft.XBOL.ClientAPI.Controllers
{
    [Route("api/orders")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly OrderService _orderService;

        public OrderController(OrderService orderService)
        {
            _orderService = orderService;
        }

        [HttpGet("{orderId}")]
        [EndpointName("GetOrderAsync")]
        public async Task<ActionResult<OrderDTO>> GetOrderAsync([FromRoute] long orderId)
        {
            // TODO: Remove temp token
            var authHeader = Request.Headers["Authorization"].ToString();
            if (authHeader.StartsWith("Bearer "))
            {
                var token = authHeader.Substring("Bearer ".Length);
                long idClient = token == "TEST-TOKEN" ? 1 : 2;

                var result = await _orderService.GetOrderAsync(idClient, orderId);

                return Ok(result);
            }
            else
            {
                return Unauthorized();
            }
        }

        [HttpGet("renovate/{orderId}")]
        [EndpointName("GetOrderToRenovate")]
        public async Task<ActionResult<SeasonToRenovateDTO>> GetOrderToRenovateAstync([FromRoute] long orderId)
        {
            // TODO: Remove temp token
            var authHeader = Request.Headers["Authorization"].ToString();
            if (authHeader.StartsWith("Bearer "))
            {
                var token = authHeader.Substring("Bearer ".Length);
                long idClient = token == "TEST-TOKEN" ? 1 : 2;

                var result = await _orderService.GetOrderToRenovate(orderId, idClient);

                return Ok(result);
            }
            else
            {
                return Unauthorized();
            }
        }
    }
}
