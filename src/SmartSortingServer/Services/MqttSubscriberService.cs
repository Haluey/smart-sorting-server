using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet;
using SmartSortingServer.DTOs;

namespace SmartSortingServer.Services {
    public class MqttSubscriberService : BackgroundService {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHostApplicationLifetime _appLifetime;
        private IMqttClient? _mqttClient;

        public MqttSubscriberService(
            IServiceScopeFactory scopeFactory,
            IHostApplicationLifetime appLifetime) {

            _scopeFactory = scopeFactory;
            _appLifetime = appLifetime;
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken) {

            // ASP.NET Core 서버 시작 완료 대기
            await WaitForApplicationStartedAsync(
                stoppingToken
            );

            // MQTT Client 생성
            var mqttFactory = new MqttClientFactory();

            _mqttClient = mqttFactory.CreateMqttClient();

            // MQTT 연결 설정
            var mqttClientOptions = new MqttClientOptionsBuilder()
                .WithTcpServer("127.0.0.1", 1883)
                .WithClientId("SmartSortingServer")
                .WithCredentials("root", "mqtt123456")
                .Build();


            // MQTT 연결 성공 이벤트
            _mqttClient.ConnectedAsync += async e => {
                Console.WriteLine();
                Console.WriteLine("MQTT Broker 연결 성공");

                // 제품 감지 토픽 구독
                var topicFilter =
                    mqttFactory
                        .CreateTopicFilterBuilder()
                        .WithTopic(
                            "smart_sorting/vision/product_detection"
                        )
                        .Build();

                var subscribeOptions =
                    mqttFactory
                        .CreateSubscribeOptionsBuilder()
                        .WithTopicFilter(topicFilter)
                        .Build();

                await _mqttClient.SubscribeAsync(
                    subscribeOptions,
                    stoppingToken
                );

                Console.WriteLine(
                    "MQTT Topic 구독 완료: " +
                    "smart_sorting/vision/product_detection"
                );
            };


            // MQTT 메시지 수신 이벤트
            _mqttClient.ApplicationMessageReceivedAsync += async e => {

                string topic =
                    e.ApplicationMessage.Topic;

                // MQTT Payload 문자열 변환
                string payload =
                    e.ApplicationMessage.ConvertPayloadToString();

                Console.WriteLine(
                    $"MQTT 메시지 수신 [{topic}]"
                );

                Console.WriteLine(payload);


                // 제품 감지 토픽 처리
                if (topic ==
                    "smart_sorting/vision/product_detection") {

                    await HandleProductDetectionAsync(
                        payload
                    );
                }
            };


            try {
                // MQTT Broker 연결
                await _mqttClient.ConnectAsync(
                    mqttClientOptions,
                    stoppingToken
                );
            }
            catch (Exception ex) {
                Console.WriteLine(
                    $"MQTT 연결 실패: {ex.Message}"
                );
            }


            // BackgroundService 유지
            try {
                await Task.Delay(
                    Timeout.Infinite,
                    stoppingToken
                );
            }
            catch (OperationCanceledException) {
                // 서버 종료 시 정상적으로 취소됨
            }
        }

        // ASP.NET Core 서버 시작 완료 대기
        private async Task WaitForApplicationStartedAsync(
            CancellationToken stoppingToken) {

            if (_appLifetime.ApplicationStarted.IsCancellationRequested) {
                return;
            }

            var startedTask = Task.Delay(
                Timeout.Infinite,
                _appLifetime.ApplicationStarted
            );

            var stoppingTask = Task.Delay(
                Timeout.Infinite,
                stoppingToken
            );

            await Task.WhenAny(
                startedTask,
                stoppingTask
            );

            stoppingToken.ThrowIfCancellationRequested();
        }

        // 제품 감지 MQTT 메시지 처리
        private async Task HandleProductDetectionAsync(
            string payload) {

            try {
                var request =
                    JsonSerializer
                        .Deserialize<ProductDetectionRequest>(
                            payload,
                            new JsonSerializerOptions {
                                PropertyNameCaseInsensitive = true
                            }
                        );

                if (request == null) {
                    Console.WriteLine(
                        "MQTT 제품 감지 데이터 변환 실패"
                    );

                    return;
                }


                // Scoped Service 생성
                using var scope =
                    _scopeFactory.CreateScope();

                var productDetectionService =
                    scope.ServiceProvider
                        .GetRequiredService<
                            ProductDetectionService
                        >();


                var result =
                    await productDetectionService
                        .CreateProductDetectionAsync(
                            request
                        );


                Console.WriteLine(
                    $"제품 감지 저장 완료: " +
                    $"{result.ProductDetectionId}"
                );
            }
            catch (Exception ex) {
                Console.WriteLine(
                    $"MQTT 제품 감지 처리 실패: " +
                    $"{ex.Message}"
                );
            }
        }
    }
}