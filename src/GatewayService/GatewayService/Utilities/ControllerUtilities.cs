using GatewayService.Dtos;

namespace GatewayService.Utilities
{
    public static class ControllerUtilities
    {
        public static CreateReadingBrokerDto ConvertToReadingsMqttDto(string deviceName, CreateReadingDto readingDto)
        {
            var timestampUtc = DateTimeOffset.FromUnixTimeSeconds(readingDto.Utc).UtcDateTime.ToString("o");

            return new CreateReadingBrokerDto(
                deviceName,
                readingDto.TemperatureC,
                readingDto.Pm25,
                timestampUtc);
        }
    }
}
