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
            // TODO: Get clientId from claims
            var result = await _orderService.GetOrderAsync(1, orderId);

            return Ok(result);
        }
    }
}
