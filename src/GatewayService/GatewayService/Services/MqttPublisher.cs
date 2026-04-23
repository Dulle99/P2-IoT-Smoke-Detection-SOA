using MQTTnet;
using System.Text;

namespace GatewayService.Services
{
    public class MqttPublisher
    {
        private readonly IMqttClient _mqttClient;
        private readonly string _topic;
        private readonly string _host;
        private readonly int _port;

        public MqttPublisher()
        {
            _host = Environment.GetEnvironmentVariable("MQTT_BROKER_HOST") ?? "mosquitto";
            _port = int.TryParse(Environment.GetEnvironmentVariable("MQTT_BROKER_PORT"), out var p) ? p : 1883;
            _topic = Environment.GetEnvironmentVariable("MQTT_TOPIC_READINGS") ?? "iot/smoke/readings";


            var factory = new MqttClientFactory();
            _mqttClient = factory.CreateMqttClient();
        }

        private async Task EnsureConnectedAsync(CancellationToken ctoken)
        {
            if (!_mqttClient.IsConnected)
            {
                var options = new MqttClientOptionsBuilder()
                    .WithTcpServer(_host, _port)
                    .Build();

                await _mqttClient.ConnectAsync(options,ctoken);
            }
        }

        public async Task PublishJsonAsync(string jsonPayload, CancellationToken ctoken)
        {
            await EnsureConnectedAsync(ctoken);

            var message = new MqttApplicationMessageBuilder()
                .WithTopic(_topic)
                .WithPayload(Encoding.UTF8.GetBytes(jsonPayload))
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();

            await _mqttClient.PublishAsync(message, ctoken);
        }
    }
}
