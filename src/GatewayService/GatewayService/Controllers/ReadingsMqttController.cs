using GatewayService.Dtos;
using GatewayService.Services;
using GatewayService.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace GatewayService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReadingsMqttController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly MqttPublisher _mqttPublisher;
        private readonly IConfiguration _config;

        public ReadingsMqttController(HttpClient httpClient, MqttPublisher mqttPublisher, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _mqttPublisher = mqttPublisher;
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

        // POST /api/readingsmqtt/{deviceId} --> Forward to Data Service POST /readings and then publish to MQTT
        [HttpPost("{deviceId}")]
        public async Task<IActionResult> Create([FromRoute] string deviceId, [FromBody] CreateReadingDto dto, CancellationToken ctoken)
        {
            var url = $"{DataServiceBaseUrl()}/readings";
            var response = await _httpClient.PostAsJsonAsync(url, dto);
            var body = await response.Content.ReadAsStringAsync(ctoken);

            if (response.IsSuccessStatusCode)
            {
                try
                {
                    var json = JsonSerializer.Serialize(ControllerUtilities.ConvertToReadingsMqttDto(deviceId, dto));
                    await _mqttPublisher.PublishJsonAsync(json, ctoken);

                    var savedReading = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ctoken);
                    return StatusCode(StatusCodes.Status201Created, new { message = "Reading forwarded to data service and published to MQTT broker successfully.", body= savedReading});
                }
                catch (Exception ex)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Failed to publish reading to MQTT broker.", error = ex.Message });
                }
            }
            else
            {
                return StatusCode((int)response.StatusCode, new { message = "Failed to forward reading to data service.", error = body });
            }
        }
    }
}
