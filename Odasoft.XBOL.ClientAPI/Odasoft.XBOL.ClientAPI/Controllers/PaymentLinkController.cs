using Microsoft.AspNetCore.Mvc;
using Odasoft.XBOL.Business;
using Odasoft.XBOL.Business.Exceptions;
using Odasoft.XBOL.Business.Services;
using Odasoft.XBOL.DTO;

namespace Odasoft.XBOL.ClientAPI.Controllers
{
    [Route("api/payment-links")]
    [ApiController]
    public class PaymentLinkController : ControllerBase
    {
        private readonly PaymentLinkService _paymentLinkService;

        public PaymentLinkController(
            PaymentLinkService paymentLinkService
        )
        {
            _paymentLinkService = paymentLinkService;
        }

        [HttpGet("{code}")]
        [EndpointName("GetAsync")]
        public async Task<ActionResult<OrderDTO>> GetAsync([FromRoute] string code)
        {
            try
            {
                var order = await _paymentLinkService.GetOrderToPayAsync(code);

                if (order == null)
                {
                    return NotFound("Order not found.");
                }

                return Ok(order);
            }
            catch (PaymentLinkExpiredException ex)
            {
                return Problem(
                    title: "Link expirado",
                    detail: ex.Message,
                    statusCode: StatusCodes.Status410Gone
                );
            }
            catch (PaymentLinkCanceledException ex)
            {
                return Problem(
                    title: "Link cancelado",
                    detail: ex.Message,
                    statusCode: StatusCodes.Status409Conflict
                );
            }
            catch (PaymentLinkAlreadyUsedException ex)
            {
                return Problem(
                    title: "Link ya utilizado",
                    detail: ex.Message,
                    statusCode: StatusCodes.Status409Conflict
                );
            }
        }

        [HttpPost("pay/{code}")]
        [EndpointName("PayOrderAsync")]
        public async Task<ActionResult<OrderDTO>> PayOrderAsync([FromRoute] string code, [FromBody] PaymentInfoRequest paymentInfoRequest)
        {
            try
            {
                var orderId = await _paymentLinkService.PayOrderAsync(code, paymentInfoRequest);
                return Ok(orderId);
            }
            catch (Exception ex)
            {
                return Problem(
                    title: ex.Message,
                    detail: ex.Message,
                    statusCode: StatusCodes.Status409Conflict
                );
            }
        }

        [HttpPost("initiate-checkout/{code}")]
        [EndpointName("InitiateCheckoutAsync")]
        public async Task<ActionResult<InitiatePaymentLinkCheckoutResponse>> InitiateCheckoutAsync(
            [FromRoute] string code,
            [FromBody] InitiatePaymentLinkCheckoutRequest request)
        {
            try
            {
                var response = await _paymentLinkService.InitiateCheckoutAsync(code, request);
                return Ok(response);
            }
            catch (PaymentLinkExpiredException ex)
            {
                return Problem(title: "Link expirado", detail: ex.Message, statusCode: StatusCodes.Status410Gone);
            }
            catch (PaymentLinkCanceledException ex)
            {
                return Problem(title: "Link cancelado", detail: ex.Message, statusCode: StatusCodes.Status409Conflict);
            }
            catch (PaymentLinkAlreadyUsedException ex)
            {
                return Problem(title: "Link ya utilizado", detail: ex.Message, statusCode: StatusCodes.Status409Conflict);
            }
            catch (Exception ex)
            {
                return Problem(title: ex.Message, detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
        }

        [HttpPost("confirm-checkout/{code}")]
        [EndpointName("ConfirmCheckoutAsync")]
        public async Task<ActionResult<long>> ConfirmCheckoutAsync(
            [FromRoute] string code,
            [FromBody] ConfirmPaymentLinkCheckoutRequest request)
        {
            try
            {
                var orderId = await _paymentLinkService.ConfirmCheckoutAsync(code, request);
                return Ok(orderId);
            }
            catch (PaymentLinkAlreadyUsedException ex)
            {
                return Problem(title: "Link ya utilizado", detail: ex.Message, statusCode: StatusCodes.Status409Conflict);
            }
            catch (Exception ex)
            {
                return Problem(title: ex.Message, detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
        }

        [HttpGet("metadata/{code}")]
        [EndpointName("GetEventMetadataByPaymentCodeAsync")]
        public async Task<ActionResult<SeoMetadataDTO>> GetEventMetadataByPaymentCodeAsync([FromRoute] string code)
        {
            var orderId = await _paymentLinkService.GetEventMetadataByPaymentCodeAsync(code);
            return Ok(orderId);
        }
    }
}
