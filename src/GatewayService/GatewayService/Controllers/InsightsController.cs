using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace GatewayService.Controllers
{
    [Route("api/insights")]
    [ApiController]
    public class InsightsController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;

        public InsightsController(HttpClient httpClient, IConfiguration configuration)
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

        // GET /api/insights/curent?limit={limit}
        //Returns: latest readings + current weather insights(Open-Meteo)
        [HttpGet("current")]
        public async Task<IActionResult> Current([FromQuery]int limit = 1)
        {
            //(1) Get latest reading from Data Service
            var readingsUrl = $"{DataServiceBaseUrl()}/readings?limit={limit}";
            var response = await _httpClient.GetAsync(readingsUrl);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode, body);

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            if(root.ValueKind != JsonValueKind.Array || root.GetArrayLength() == 0)
            {
                return NotFound("No readings found in DataService.");
            }

            //2) Call external weather API (Open-Meteo)
            var weatherUrl = "https://api.open-meteo.com/v1/forecast?latitude=44.8&longitude=20.5&current=temperature_2m,relative_humidity_2m,wind_speed_10m";
            var weatherJson = await _httpClient.GetStringAsync(weatherUrl);
            using var weatherDoc = JsonDocument.Parse(weatherJson);
            var weatherEl = weatherDoc.RootElement.Clone(); // clone because weatherDoc will be disposed

            //3) Integrate data
            var integratedData = new List<object>();

            foreach (var element in root.EnumerateArray())
            {
                integratedData.Add(new
                {
                    latestReading = element.Clone(), // clone because readingsDoc will be disposed
                    weather = weatherEl
                });
            }

            //4) Return integrated response

            return Ok(integratedData);
        }
    }
}
