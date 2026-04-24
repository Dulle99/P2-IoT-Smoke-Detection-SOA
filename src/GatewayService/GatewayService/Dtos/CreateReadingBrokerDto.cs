namespace GatewayService.Dtos
{
    public record CreateReadingBrokerDto
    (
        string deviceId,
        double temperature,
        double pm25,
        string timestampUtc
    );
}
