namespace AnalyticsService.Dtos
{

    public record ReadingDto
    (
        string DeviceId,
        double Temperature,
        double Pm25,
        string TimestampUtc
    );

}
