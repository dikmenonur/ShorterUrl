using Microsoft.AspNetCore.Mvc;

namespace ShorterUrl.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ShorterUrlController : ControllerBase
    {
        private readonly ILogger<ShorterUrlController> _logger;

        public ShorterUrlController(ILogger<ShorterUrlController> logger)
        {
            _logger = logger;
        }

        [HttpGet(Name = "getShotURL")]
        public void Get()
        {

        }

        [HttpPost(Name = "createShotURL")]
        public void CreateShotURL()
        {

        }
    }
}
