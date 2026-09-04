using Microsoft.Extensions.DependencyInjection;
using MQTTnet;
using SmartSortingServer.Data;
using SmartSortingServer.DTOs;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace SmartSortingServer.Services {
    public class MqttSubscriberService : BackgroundService {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHostApplicationLifetime _appLifetime;
        private readonly ILogger<MqttSubscriberService> _logger;
        private IMqttClient? _mqttClient;

        public MqttSubscriberService(
            IServiceScopeFactory scopeFactory,
            IHostApplicationLifetime appLifetime,
            ILogger<MqttSubscriberService> logger) {

            _scopeFactory = scopeFactory;
            _appLifetime = appLifetime;
            _logger = logger;
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

                _logger.LogInformation(
                    "\n[MQTT] Broker 연결 성공"
                );

                // 제품 감지 토픽 구독
                var topicFilter =
                    mqttFactory
                        .CreateTopicFilterBuilder()
                        .WithTopic(
                            "smart_sorting/camera/product_detection"
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

                // 컴포넌트 상태 토픽 구독
                var componentStatusTopicFilter =
                    mqttFactory
                        .CreateTopicFilterBuilder()
                        .WithTopic(
                            "smart_sorting/component/status/update"
                        )
                        .Build();

                var componentStatusSubscribeOptions =
                    mqttFactory
                        .CreateSubscribeOptionsBuilder()
                        .WithTopicFilter(componentStatusTopicFilter)
                        .Build();

                await _mqttClient.SubscribeAsync(
                    componentStatusSubscribeOptions,
                    stoppingToken
                );

                _logger.LogInformation(
                    "[MQTT] Subscribe - Topic: {Topic}",
                    "smart_sorting/camera/product_detection"
                );

                _logger.LogInformation(
                    "[MQTT] Subscribe - Topic: {Topic}",
                    "smart_sorting/component/status/update\n"
                );
            };


            // MQTT 메시지 수신 이벤트
            _mqttClient.ApplicationMessageReceivedAsync += async e => {

                string topic =
                    e.ApplicationMessage.Topic;

                // MQTT Payload 문자열 변환
                string payload =
                    e.ApplicationMessage.ConvertPayloadToString();

                _logger.LogInformation(
                    "[MQTT] Receive - Topic: {Topic}",
                    topic
                );

                // 제품 감지 토픽 처리
                if (topic ==
                    "smart_sorting/camera/product_detection") {

                    await HandleProductDetectionAsync(
                        payload
                    );
                }

                // 컴포넌트 상태 토픽 처리
                else if (topic ==
                    "smart_sorting/component/status/update") {

                    await HandleComponentStatusUpdateAsync(
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
                _logger.LogError(
                    "[MQTT] Broker 연결 실패 - {Message}",
                    ex.Message
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
        private async Task HandleProductDetectionAsync(string payload) {

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
                    _logger.LogError(
                        "[MQTT] 제품 감지 데이터 변환 실패"
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


                await productDetectionService
                    .CreateProductDetectionAsync(
                        request
                    );

            }
            catch (Exception ex) {
                _logger.LogError(
                    "[MQTT] 제품 감지 처리 실패 - {Message}",
                    ex.Message
                );
            }
        }

        // 컴포넌트 상태 MQTT 메시지 처리
        private async Task HandleComponentStatusUpdateAsync(string payload) {

            try {
                var request =
                    JsonSerializer
                        .Deserialize<ComponentStatusUpdateRequest>(
                            payload,
                            new JsonSerializerOptions {
                                PropertyNameCaseInsensitive = true
                            }
                        );

                if (request == null) {
                    _logger.LogError(
                        "[MQTT] 컴포넌트 상태 데이터 변환 실패"
                    );

                    return;
                }

                // 입력값 정리
                string componentCode =
                    request.ComponentCode
                        .Trim()
                        .ToUpper();

                string status =
                    request.Status
                        .Trim()
                        .ToUpper();

                string? errorCode =
                    string.IsNullOrWhiteSpace(request.ErrorCode)
                        ? null
                        : request.ErrorCode.Trim().ToUpper();


                // 상태값 확인
                string[] allowedStatuses = {
                    "NORMAL",
                    "WARNING",
                    "ERROR",
                    "OFFLINE"
                };

                if (!allowedStatuses.Contains(status)) {
                    _logger.LogError(
                        "[MQTT] 잘못된 컴포넌트 상태 - Component: {ComponentCode}, Status: {Status}",
                        componentCode,
                        status
                    );

                    return;
                }


                // NORMAL이면 ErrorCode가 없어야 함
                if (status == "NORMAL" &&
                    errorCode != null) {

                    _logger.LogError(
                        "[MQTT] NORMAL 상태에는 ErrorCode를 사용할 수 없음 - Component: {ComponentCode}, ErrorCode: {ErrorCode}",
                        componentCode,
                        errorCode
                    );

                    return;
                }


                // 비정상 상태이면 ErrorCode가 필요함
                if (status != "NORMAL" &&
                    errorCode == null) {

                    _logger.LogError(
                        "[MQTT] 비정상 상태에는 ErrorCode가 필요함 - Component: {ComponentCode}, Status: {Status}",
                        componentCode,
                        status
                    );

                    return;
                }


                // Scoped Service 생성
                using var scope =
                    _scopeFactory.CreateScope();

                var context =
                    scope.ServiceProvider
                        .GetRequiredService<AppDbContext>();

                var componentAlertService =
                    scope.ServiceProvider
                        .GetRequiredService<ComponentAlertService>();

                var mqttPublisher =
                    scope.ServiceProvider
                        .GetRequiredService<MqttPublisherService>();


                // 컴포넌트 조회
                var component =
                    await context.SystemComponents
                        .FirstOrDefaultAsync(
                            c => c.ComponentCode
                                == componentCode
                        );

                if (component == null) {
                    _logger.LogError(
                        "[MQTT] 시스템 컴포넌트를 찾을 수 없음 - Component: {ComponentCode}",
                        componentCode
                    );

                    return;
                }


                // 비정상 상태인 경우 ErrorCode 기반 정보 생성
                if (status != "NORMAL" &&
                    errorCode != null) {

                    var alertInfo =
                        componentAlertService
                            .GetAlertInfo(errorCode);

                    if (alertInfo.ComponentCode == "UNKNOWN") {
                        _logger.LogError(
                            "[MQTT] 정의되지 않은 ErrorCode - Component: {ComponentCode}, ErrorCode: {ErrorCode}",
                            componentCode,
                            errorCode
                        );

                        return;
                    }

                    if (alertInfo.ComponentCode != componentCode) {
                        _logger.LogError(
                            "[MQTT] Component와 ErrorCode 불일치 - Component: {ComponentCode}, ErrorCode: {ErrorCode}, ExpectedComponent: {ExpectedComponent}",
                            componentCode,
                            errorCode,
                            alertInfo.ComponentCode
                        );

                        return;
                    }

                    if (alertInfo.Status != status) {
                        _logger.LogError(
                            "[MQTT] Status와 ErrorCode 불일치 - Component: {ComponentCode}, Status: {Status}, ErrorCode: {ErrorCode}, ExpectedStatus: {ExpectedStatus}",
                            componentCode,
                            status,
                            errorCode,
                            alertInfo.Status
                        );

                        return;
                    }

                    _logger.LogInformation(
                        "[COMPONENT] 오류 정보 - Component: {ComponentCode}, ErrorCode: {ErrorCode}, Message: {Message}, Priority: {Priority}",
                        componentCode,
                        errorCode,
                        alertInfo.DetailMessage,
                        alertInfo.Priority
                    );

                    var createdAlert =
                        await componentAlertService
                            .CreateComponentAlertAsync(
                                component,
                                errorCode
                            );

                    if (createdAlert != null) {
                        _logger.LogInformation(
                            "[ALERT] 컴포넌트 알림 생성 - AlertId: {AlertId}, Component: {ComponentCode}, ErrorCode: {ErrorCode}",
                            createdAlert.AlertId,
                            componentCode,
                            errorCode
                        );

                        await mqttPublisher.PublishAsync(
                            "smart_sorting/alert",
                            new {
                                alertId = createdAlert.AlertId,
                                alertType = createdAlert.AlertType,
                                priority = createdAlert.Priority,
                                componentCode = componentCode,
                                errorCode = errorCode,

                                // Qt 작업자 화면용 짧은 메시지
                                shortMessage = alertInfo.ShortMessage,

                                alertMessage = alertInfo.DetailMessage,

                                createdAt = createdAlert.CreatedAt
                            }
                        );
                    }
                    else {
                        _logger.LogInformation(
                            "[ALERT] 동일한 미복구 알림이 이미 존재함 - Component: {ComponentCode}, ErrorCode: {ErrorCode}",
                            componentCode,
                            errorCode
                        );
                    }
                }

                // NORMAL 상태인 경우 기존 미복구 Alert 복구 처리
                if (status == "NORMAL") {

                    int recoveredCount =
                        await componentAlertService
                            .RecoverComponentAlertsAsync(
                                component
                            );

                    if (recoveredCount > 0) {
                        _logger.LogInformation(
                            "[ALERT] 컴포넌트 알림 복구 - Component: {ComponentCode}, Count: {Count}",
                            componentCode,
                            recoveredCount
                        );
                    }
                }

                // 기존 상태 저장
                string previousStatus =
                    component.CurrentStatus;

                // 상태 변경
                component.CurrentStatus =
                    status;

                component.StatusUpdatedAt =
                    DateTime.Now;

                await context.SaveChangesAsync();


                // 실제 상태가 변경된 경우 로그 출력
                if (previousStatus != component.CurrentStatus) {
                    _logger.LogInformation(
                        "[COMPONENT] 상태 변경 - Component: {ComponentCode}, {PreviousStatus} -> {CurrentStatus}",
                        component.ComponentCode,
                        previousStatus,
                        component.CurrentStatus
                    );

                    await mqttPublisher.PublishAsync(
                       "smart_sorting/component/status",
                       new {
                           componentCode = component.ComponentCode,
                           status = component.CurrentStatus
                       }
                   );
                }
            }
            catch (Exception ex) {
                _logger.LogError(
                    "[MQTT] 컴포넌트 상태 처리 실패 - {Message}",
                    ex.Message
                );
            }
        }

    }
}