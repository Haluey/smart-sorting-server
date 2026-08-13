using System.Text.Json;
using MQTTnet;

namespace SmartSortingServer.Services {
    public class MqttPublisherService {
        private readonly IMqttClient _mqttClient;
        private readonly MqttClientOptions _mqttClientOptions;

        public MqttPublisherService() {
            var mqttFactory = new MqttClientFactory();

            _mqttClient = mqttFactory.CreateMqttClient();

            _mqttClientOptions = new MqttClientOptionsBuilder()
                .WithTcpServer("127.0.0.1", 1883)
                .WithClientId("SmartSortingServerPublisher")
                .WithCredentials(
                    "root",
                    "mqtt123456"
                )
                .Build();
        }

        public async Task PublishAsync(
            string topic,
            object payload) {
            // MQTT Broker와 연결되어 있지 않으면 연결
            if (!_mqttClient.IsConnected) {
                await _mqttClient.ConnectAsync(
                    _mqttClientOptions,
                    CancellationToken.None
                );
            }

            // 객체를 JSON 문자열로 변환
            var json = JsonSerializer.Serialize(payload);

            // MQTT 메시지 생성
            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(json)
                .Build();

            // MQTT Publish
            await _mqttClient.PublishAsync(
                message,
                CancellationToken.None
            );

            Console.WriteLine(
                $"MQTT Publish [{topic}]"
            );

            Console.WriteLine(json);
        }
    }
}