namespace GatewayService.Dtos
{
    public record CreateReadingBrokerDto(
        string deviceId,
        double temperature,
        double smokeLevel,
        string timestampUtc
    );
}