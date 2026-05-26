using Microsoft.AspNetCore.Mvc;
using Odasoft.XBOL.Business.Services;
using Odasoft.XBOL.ClientAPI.Services;
using Odasoft.XBOL.DTO;

namespace Odasoft.XBOL.ClientAPI.Controllers
{
    [Route("api/seasons")]
    [ApiController]
    public class SeasonController : ControllerBase
    {
        private readonly SeasonService _seasonService;
        private readonly IClientIdentityService _clientIdentityService;

        public SeasonController(SeasonService seasonService, IClientIdentityService clientIdentityService)
        {
            _seasonService = seasonService;
            _clientIdentityService = clientIdentityService;
        }

        [HttpGet]
        [EndpointName("GetSeasonBannerAsync")]
        public async Task<ActionResult<SeasonItemDTO?>> GetSeasonBannerAsync()
        {
            var client = await _clientIdentityService.TryResolveCurrentClientAsync(User);

            var result = await _seasonService.GetSeasonBannerAsync(client?.Id);
            return Ok(result);
        }
    }
}
