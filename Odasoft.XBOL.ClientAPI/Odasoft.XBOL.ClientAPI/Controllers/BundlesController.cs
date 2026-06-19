using Microsoft.AspNetCore.Mvc;
using Odasoft.XBOL.Business.Services;
using Odasoft.XBOL.ClientAPI.Services;
using Odasoft.XBOL.DTO;

namespace Odasoft.XBOL.ClientAPI.Controllers
{
    [Route("api/bundles")]
    [ApiController]
    public class BundlesController : ControllerBase
    {
        private readonly BundleService _bundleService;
        private readonly IClientIdentityService _clientIdentityService;

        public BundlesController(BundleService bundleService, IClientIdentityService clientIdentityService)
        {
            _bundleService = bundleService;
            _clientIdentityService = clientIdentityService;
        }

        [HttpGet]
        [EndpointName("GetBundleBannerAsync")]
        public async Task<ActionResult<BundleItemDTO?>> GetBundleBannerAsync(
            [FromQuery] bool includeMedia = false)
        {
            var client = await _clientIdentityService.TryResolveCurrentClientAsync(User);

            var result = await _bundleService.GetBundleBannerAsync(client?.Id, includeMedia);
            return Ok(result);
        }

        [HttpGet("{bundleId}/metadata")]
        [EndpointName("GetBundleMetadataAsync")]
        [ProducesResponseType(typeof(SeoMetadataDTO), StatusCodes.Status200OK)]
        public async Task<ActionResult<SeoMetadataDTO>> GetBundleMetadataAsync(
            [FromRoute] long bundleId)
        {
            var result = await _bundleService.GetBundleMetadataAsync(bundleId);

            return Ok(result);
        }
    }
}
