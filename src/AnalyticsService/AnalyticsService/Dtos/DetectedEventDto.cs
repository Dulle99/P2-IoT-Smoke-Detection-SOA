namespace AnalyticsService.Dtos
{
    public record DetectedEventDto
    (
        string Type,
        string DeviceId,
        double SmokeLevel,
        double Temperature,
        DateTime TimestampUtc

    );
}
