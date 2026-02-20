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

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var factory = new MqttClientFactory();
            _mqttClient = factory.CreateMqttClient();

            var options = new MqttClientOptionsBuilder()
                .WithTcpServer("mosquitto", 1883)
                .WithClientId("analytics-service")
                .Build();

            _mqttClient.ApplicationMessageReceivedAsync += e =>
            {
                var payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                try
                {
                    var reading = JsonSerializer.Deserialize<ReadingDto>(payload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true});
                    if(reading == null)
                    {
                        Console.WriteLine($"Invalid JSON payload");
                        return Task.CompletedTask;
                    }

                    Console.WriteLine($"Received reading: DeviceId={reading.DeviceId}, Temperature={reading.Temperature}, SmokeLevel={reading.SmokeLevel}, Timestamp={reading.TimestampUtc}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error deserializing message: {ex.Message}");
                }
                return Task.CompletedTask;
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
