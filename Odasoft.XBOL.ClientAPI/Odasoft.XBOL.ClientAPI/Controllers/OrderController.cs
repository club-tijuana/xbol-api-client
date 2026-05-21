using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Odasoft.XBOL.Business.Services;
using Odasoft.XBOL.ClientAPI.Services;
using Odasoft.XBOL.DTO;

namespace Odasoft.XBOL.ClientAPI.Controllers
{
    [Route("api/orders")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly OrderService _orderService;
        private readonly ILogger<OrderController> _logger;
        private readonly IClientIdentityService _clientIdentityService;

        public OrderController(
            OrderService orderService,
            ILogger<OrderController> logger,
            IClientIdentityService clientIdentityService)
        {
            _orderService = orderService;
            _logger = logger;
            _clientIdentityService = clientIdentityService;
        }

        [HttpGet("{orderId}")]
        [Authorize]
        [EndpointName("GetOrderAsync")]
        public async Task<ActionResult<OrderDTO>> GetOrderAsync([FromRoute] long orderId)
        {
            var client = await _clientIdentityService.RequireCurrentClientAsync(User);
            var result = await _orderService.GetOrderAsync(client.Id, orderId);

            return Ok(result);
        }

        [HttpGet("renovate/{orderId}")]
        [Authorize]
        [EndpointName("GetOrderToRenovate")]
        public async Task<ActionResult<SeasonToRenovateDTO>> GetOrderToRenovateAstync([FromRoute] long orderId)
        {
            var client = await _clientIdentityService.RequireCurrentClientAsync(User);

            try
            {
                var result = await _orderService.GetOrderToRenovate(orderId, client.Id);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get order to renovate {OrderId}", orderId);
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Evaluates whether a specific order is eligible for renewal.
        /// </summary>
        /// <param name="orderReference">The unique reference identifier of the order.</param>
        /// <returns>An ActionResult containing order information indicating whether the order can be renewed.</returns>
        [HttpGet("{orderReference}/can-renew")]
        [EndpointName("CanOrderBeRenewedAsync")]
        [ProducesResponseType(typeof(CanRenewOrderResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<CanRenewOrderResponse>> CanOrderBeRenewedAsync([FromRoute] string orderReference)
        {
            CanRenewOrderResponse canRenew = await _orderService.CanOrderBeRenewedAsync(orderReference);

            if (canRenew == null || canRenew.OrderId == null || canRenew.OrderId == 0)
            {
                return NotFound($"There is no information for Order '{orderReference}'.");
            }

            return Ok(canRenew);
        }
    }
}
