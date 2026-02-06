using Microsoft.AspNetCore.Mvc;
using Odasoft.XBOL.DTO;
using System.Text.Json;

namespace Odasoft.XBOL.ClientAPI.Controllers
{
    [Route("api/faqs")]
    [ApiController]
    public class FaqsController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;

        public FaqsController(IWebHostEnvironment env)
        {
            _env = env;
        }

        [HttpGet]
        [EndpointName("GetFaqsAsync")]
        public async Task<List<FaqDTO>> GetFaqsAsync()
        {
            var rootPath = _env.ContentRootPath
                ?? throw new Exception("ContentRootPath is null");

            var path = Path.Combine(rootPath, "Data", "faqs.json");

            if (!System.IO.File.Exists(path))
            {
                return new List<FaqDTO>();
            }

            var json = await System.IO.File.ReadAllTextAsync(path);

            var faqs = JsonSerializer.Deserialize<List<FaqDTO>>(json);

            return faqs ?? new List<FaqDTO>();

        }
    }
}
