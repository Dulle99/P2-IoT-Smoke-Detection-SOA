using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AnalyticsService.Dtos;
using AnalyticsService.Protos;
using Microsoft.Extensions.Hosting;
using MQTTnet;

namespace AnalyticsService.Services
{
    public class MqttSubscriberService : BackgroundService
    {
        private IMqttClient _mqttClient;
        private readonly IEventWriter _eventWriter;
        private readonly NotificationService.NotificationServiceClient _notificationClient;

        public MqttSubscriberService(IEventWriter eventWriter, NotificationService.NotificationServiceClient notificationClient)
        {
            _eventWriter = eventWriter;
            _notificationClient = notificationClient;
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

                            //grpc notification
                            var req = new SmokeAlertRequest
                            {
                                DeviceId = reading.DeviceId,
                                SmokeLevel = reading.SmokeLevel,
                                Temperature = reading.Temperature,
                                TimestampUtc = timestamp.UtcDateTime.ToString("o")
                            };

                            var resp = await _notificationClient.SendSmokeAlertAsync(req, cancellationToken: stoppingToken);
                            Console.WriteLine($"Notification response: {resp.Status}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"gRPC call failed: {ex.Message}");
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
