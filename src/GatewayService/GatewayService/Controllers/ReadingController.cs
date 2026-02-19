using GatewayService.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GatewayService.Controllers
{
    [Route("api/readings")]
    [ApiController]
    public class ReadingController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;

        public ReadingController(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _config = configuration;
        }

        private string DataServiceBaseUrl()
        {
            var baseUrl = _config["DataService:BaseUrl"];
            if (string.IsNullOrEmpty(baseUrl))
            {
                throw new InvalidOperationException("Data service base URL is not configured.");
            }

            return baseUrl.TrimEnd('/');
        }

        // POST /api/readings --> Forward to Data Service POST /readings
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateReadingDto dto)
        {
            var url = $"{DataServiceBaseUrl()}/readings";
            var response = await _httpClient.PostAsJsonAsync(url, dto);

            var body = await response.Content.ReadAsStringAsync();
            return StatusCode((int)response.StatusCode, body);
        }

        //GET /api/readings?limit={limit} --> forward to Data Service GET /readings
        [HttpGet()]
        public async Task<IActionResult> Get([FromQuery]int limit = 1)
        {
            var url = $"{DataServiceBaseUrl()}/readings?limit={limit}";
            var response = await _httpClient.GetAsync(url);
            var body = await response.Content.ReadAsStringAsync();
            return StatusCode((int)response.StatusCode, body);
        }

        //Get /api/readings/{id} --> forward to Data Service GET /readings/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById([FromRoute] string id)
        {
            var url = $"{DataServiceBaseUrl()}/readings/{id}";
            var response = await _httpClient.GetAsync(url);
            var body = await response.Content.ReadAsStringAsync();
            return StatusCode((int)response.StatusCode, body);
        }
    }
}
