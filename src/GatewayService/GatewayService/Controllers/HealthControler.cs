using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GatewayService.Controllers
{
    [Route("api/health")]
    [ApiController]
    public class HealthControler : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok("Gateway Service is healthy");
        }
    }
}
