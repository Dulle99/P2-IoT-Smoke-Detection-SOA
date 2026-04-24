using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;
using Microsoft.Extensions.Options;

namespace AnalyticsService.Services
{
    public class InfluxOptions
    {
        public string Url { get; set; } = default!;
        public string Token { get; set; } = default!;
        public string Org { get; set; } =default!;
        public string Bucket { get; set; } = default!;   
    }

    public interface IEventWriter
    {
        Task WritePM25EventAsync(string deviceId, double pm25, double temperature, DateTime timestampUtc, CancellationToken ct);
    }

    public class InfluxEventWriter : IEventWriter
    {
        private readonly InfluxDBClient _client;
        private readonly InfluxOptions _options;

        public InfluxEventWriter(IOptions<InfluxOptions> options)
        {
            _options = options.Value;
            _client = new InfluxDBClient(_options.Url, _options.Token);
        }

        public async Task WritePM25EventAsync(string deviceId, double pm25, double temperature, DateTime timestampUtc, CancellationToken ct)
        {
            var point = PointData
                .Measurement("pm25_events")
                .Tag("device_id", deviceId)
                .Field("pm25    ", pm25)
                .Field("temperature", temperature)
                .Timestamp(timestampUtc, WritePrecision.Ns);

            await _client.GetWriteApiAsync().WritePointAsync(point, _options.Bucket, _options.Org, ct);

        }
    }
}
