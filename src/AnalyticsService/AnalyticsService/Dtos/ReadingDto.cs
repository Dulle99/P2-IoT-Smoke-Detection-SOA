namespace AnalyticsService.Dtos
{

    public record ReadingDto
    (
        string DeviceId,
        double Temperature,
        double SmokeLevel,
        string TimestampUtc
    );

}
