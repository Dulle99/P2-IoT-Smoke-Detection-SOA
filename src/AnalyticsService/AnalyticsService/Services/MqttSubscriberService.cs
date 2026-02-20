using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AnalyticsService.Dtos;
using Microsoft.Extensions.Hosting;
using MQTTnet;

namespace AnalyticsService.Services
{
    public class MqttSubscriberService : BackgroundService
    {
        private IMqttClient _mqttClient;
        private readonly IEventWriter _eventWriter;

        public MqttSubscriberService(IEventWriter eventWriter)
        {
            _eventWriter = eventWriter;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var factory = new MqttClientFactory();
            _mqttClient = factory.CreateMqttClient();

            var options = new MqttClientOptionsBuilder()
                .WithTcpServer("mosquitto", 1883)
                .WithClientId("analytics-service")
                .Build();

            _mqttClient.ApplicationMessageReceivedAsync += async e =>
            {
                var payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                try
                {
                    var reading = JsonSerializer.Deserialize<ReadingDto>(payload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true});
                    if(reading == null)
                    {
                        Console.WriteLine($"Invalid JSON payload");
                        return;
                    }

                    if(!DateTimeOffset.TryParse(reading.TimestampUtc, out var timestamp))
                    {
                        Console.WriteLine($"Invalid timestamp format: {reading.TimestampUtc}");
                        return;
                    }

                    if (reading.SmokeLevel >= 70)
                    {
                        Console.WriteLine($"EVENT DETECTED: from {reading.DeviceId} (Smoke={reading.SmokeLevel})");

                        try
                        {
                            await _eventWriter.WriteSmokeEventAsync(reading.DeviceId, reading.SmokeLevel, reading.Temperature, timestamp.UtcDateTime, stoppingToken);
                            Console.WriteLine(" Event written to InfluxDB");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error writing event: {ex.Message}");
                        }
                    
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error deserializing message: {ex.Message}");
                }
                return;
            };

            await _mqttClient.ConnectAsync(options, stoppingToken);
            Console.WriteLine("Connected to MQTT broker");

            await _mqttClient.SubscribeAsync("iot/smoke/readings");
            Console.WriteLine("Subscribed to topic: iot/smoke/readings");

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
