using GatewayService.Dtos;

namespace GatewayService.Utilities
{
    public static class ControllerUtilities
    {
        public static CreateReadingBrokerDto ConvertToReadingsMqttDto (string deviceName, CreateReadingDto readingDto)
        {
            var createReadingBrokerDto = new CreateReadingBrokerDto(deviceName, readingDto.TemperatureC,readingDto.Pm25, readingDto.Utc.ToString());

            return createReadingBrokerDto;
        }
    }
}
