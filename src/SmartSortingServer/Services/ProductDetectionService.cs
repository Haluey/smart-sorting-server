using Microsoft.EntityFrameworkCore;
using SmartSortingServer.Data;
using SmartSortingServer.DTOs;
using SmartSortingServer.Models;

namespace SmartSortingServer.Services {
    public class ProductDetectionService {
        private readonly AppDbContext _context;
        private readonly MqttPublisherService _mqttPublisher;
        private readonly ILogger<ProductDetectionService> _logger;

        public ProductDetectionService(
            AppDbContext context,
            MqttPublisherService mqttPublisher,
            ILogger<ProductDetectionService> logger) {

            _context = context;
            _mqttPublisher = mqttPublisher;
            _logger = logger;
        }

        public async Task<ProductDetection> CreateProductDetectionAsync(
            ProductDetectionRequest request) {

            // 현재 진행 중인 생산 작업 조회
            var productionSession = await _context.ProductionSessions
                .Where(s =>
                    s.Status == "RUNNING"
                    || s.Status == "PAUSED")
                .OrderByDescending(s => s.StartedAt)
                .FirstOrDefaultAsync();

            if (productionSession == null) {
                throw new InvalidOperationException(
                    "진행 중인 생산 작업이 없습니다."
                );
            }

            // 분류 상태 확인
            if (request.ClassificationStatus != "SUCCESS"
                && request.ClassificationStatus != "FAILED") {

                throw new ArgumentException(
                    "분류 상태는 SUCCESS 또는 FAILED만 사용할 수 있습니다."
                );
            }

            ProductType? productType = null;

            // 이번 처리에서 생성된 알림
            Alert? createdAlert = null;

            // 이번 처리에서 완료된 초콜릿 누적 세트 수
            int? completedChocolateSetCount = null;

            // 분류 성공인 경우
            if (request.ClassificationStatus == "SUCCESS") {

                if (string.IsNullOrWhiteSpace(
                    request.ProductTypeCode)) {

                    throw new ArgumentException(
                        "분류 성공 시 제품 유형이 필요합니다."
                    );
                }

                if (request.Confidence == null
                    || request.Confidence < 0
                    || request.Confidence > 1) {

                    throw new ArgumentException(
                        "신뢰도는 0 이상 1 이하의 값이어야 합니다."
                    );
                }

                productType = await _context.ProductTypes
                    .FirstOrDefaultAsync(
                        p => p.ProductTypeCode
                            == request.ProductTypeCode
                    );

                if (productType == null) {
                    throw new ArgumentException(
                        "등록되지 않은 제품 유형입니다."
                    );
                }
            }

            // 분류 실패인 경우
            if (request.ClassificationStatus == "FAILED"
                && request.ProductTypeCode != null) {

                throw new ArgumentException(
                    "분류 실패 시 제품 유형을 지정할 수 없습니다."
                );
            }

            // 제품 감지 결과 생성
            var productDetection = new ProductDetection {
                SessionId = productionSession.SessionId,
                ProductTypeId = productType?.ProductTypeId,
                Confidence = request.Confidence,
                ImagePath = request.ImagePath,
                ClassificationStatus =
                    request.ClassificationStatus,
                DetectedAt = DateTime.Now
            };

            // 먼저 저장하여 ProductDetectionId 생성
            _context.ProductDetections.Add(productDetection);
            await _context.SaveChangesAsync();

            // -------------------------------------------------
            // 신규 제품 감지 MQTT Publish
            // -------------------------------------------------

            await _mqttPublisher.PublishAsync(
                "smart_sorting/product/detection",
                new {
                    detectionId = productDetection.ProductDetectionId,

                    classificationStatus =
                        productDetection.ClassificationStatus,

                    productTypeCode =
                        productType?.ProductTypeCode,

                    confidence =
                        productDetection.Confidence,

                    imagePath =
                        productDetection.ImagePath,

                    detectedAt =
                        productDetection.DetectedAt
                }
            );

            // -------------------------------------------------
            // 분류 성공 처리
            // -------------------------------------------------

            if (request.ClassificationStatus == "SUCCESS"
                && productType != null) {

                // 초콜릿
                if (productType.ProductTypeCode == "CHOCOLATE") {

                    DateTime today = DateTime.Today;
                    DateTime tomorrow = today.AddDays(1);

                    // 이번 감지 전 오늘 전체 초콜릿 생산 개수
                    int totalChocolateCountBefore =
                        await _context.ProductionSessions
                            .Where(s =>
                                s.StartedAt >= today
                                && s.StartedAt < tomorrow
                            )
                            .SumAsync(s => s.ChocolateCount);

                    // 현재 작업자의 생산량 증가
                    productionSession.ChocolateCount += 1;

                    // 이번 감지를 포함한 오늘 전체 누적 생산량
                    int totalChocolateCount =
                        totalChocolateCountBefore + 1;

                    // 초콜릿 세트 완료 INFO 알림 생성
                    if (productType.UnitPerSet > 0
                        && totalChocolateCount
                            % productType.UnitPerSet == 0) {

                        int completedSetCount =
                            totalChocolateCount
                            / productType.UnitPerSet;

                        completedChocolateSetCount =
                            completedSetCount;

                        var infoAlert = new Alert {
                            SessionId =
                                productionSession.SessionId,

                            ComponentId = null,

                            ProductDetectionId =
                                productDetection.ProductDetectionId,

                            CheckedByUserId = null,

                            AlertType = "INFO",
                            Priority = "LOW",

                            RecoveryStatus = null,
                            CheckStatus = null,

                            AlertMessage =
                                $"초콜릿 {completedSetCount}세트 생산이 완료되었습니다.",

                            CreatedAt = DateTime.Now,
                            RecoveredAt = null,
                            CheckedAt = null
                        };

                        _context.Alerts.Add(infoAlert);

                        createdAlert = infoAlert;
                    }
                }

                // 사탕
                else if (productType.ProductTypeCode == "CANDY") {

                    productionSession.CandyCount += 1;
                }

                productionSession.UpdatedAt = DateTime.Now;
            }

            // 수량 및 알림 변경 저장
            await _context.SaveChangesAsync();

            if (request.ClassificationStatus == "SUCCESS"
                && productType != null) {

                _logger.LogInformation(
                    "[DETECTION] 제품 분류 성공 - DetectionId: {DetectionId}, ProductType: {ProductType}, Confidence: {Confidence}",
                    productDetection.ProductDetectionId,
                    productType.ProductTypeCode,
                    productDetection.Confidence
                );
            }

            if (request.ClassificationStatus == "FAILED") {

                _logger.LogWarning(
                    "[DETECTION] 제품 분류 실패 - DetectionId: {DetectionId}",
                    productDetection.ProductDetectionId
                );
            }

            // -------------------------------------------------
            // 생산 현황 MQTT Publish
            // -------------------------------------------------

            // 분류 성공으로 생산량이 변경된 경우에만 전송
            if (request.ClassificationStatus == "SUCCESS"
                && productType != null) {

                // 초콜릿 제품 유형 조회
                var chocolateType =
                    await _context.ProductTypes
                        .FirstOrDefaultAsync(
                            p => p.ProductTypeCode
                                == "CHOCOLATE"
                        );

                if (chocolateType == null) {
                    throw new InvalidOperationException(
                        "초콜릿 제품 유형 정보가 없습니다."
                    );
                }

                // 사탕 제품 유형 조회
                var candyType =
                    await _context.ProductTypes
                        .FirstOrDefaultAsync(
                            p => p.ProductTypeCode
                                == "CANDY"
                        );

                if (candyType == null) {
                    throw new InvalidOperationException(
                        "사탕 제품 유형 정보가 없습니다."
                    );
                }

                // 초콜릿 목표 개수
                int chocolateTargetCount =
                    productionSession.TargetChocolateSetCount
                    * chocolateType.UnitPerSet;

                // 사탕 목표 개수
                int candyTargetCount =
                    productionSession.TargetCandySetCount
                    * candyType.UnitPerSet;

                // 초콜릿 현재 세트 수
                int chocolateSetCount =
                    chocolateType.UnitPerSet > 0
                        ? productionSession.ChocolateCount
                            / chocolateType.UnitPerSet
                        : 0;

                // 초콜릿 진행률
                int chocolateProgress =
                    chocolateTargetCount > 0
                        ? (int)Math.Round(
                            (double)productionSession.ChocolateCount
                            / chocolateTargetCount
                            * 100
                        )
                        : 0;

                // 사탕 현재 세트 수
                int candySetCount =
                    candyType.UnitPerSet > 0
                        ? productionSession.CandyCount
                            / candyType.UnitPerSet
                        : 0;

                // 사탕 진행률
                int candyProgress =
                    candyTargetCount > 0
                        ? (int)Math.Round(
                            (double)productionSession.CandyCount
                            / candyTargetCount
                            * 100
                        )
                        : 0;

                // 생산 현황 MQTT 전송
                await _mqttPublisher.PublishAsync(
                    "smart_sorting/production/status",
                    new {
                        sessionId =
                            productionSession.SessionId,

                        status =
                            productionSession.Status,

                        chocolate = new {
                            currentCount =
                                productionSession.ChocolateCount,

                            targetCount =
                                chocolateTargetCount,

                            unitPerSet =
                                chocolateType.UnitPerSet,

                            setCount =
                                chocolateSetCount,

                            progress =
                                chocolateProgress
                        },

                        candy = new {
                            currentCount =
                                productionSession.CandyCount,

                            targetCount =
                                candyTargetCount,

                            unitPerSet =
                                candyType.UnitPerSet,

                            setCount =
                                candySetCount,

                            progress =
                                candyProgress
                        }
                    }
                );
            }

            // -------------------------------------------------
            // 신규 알림 MQTT Publish
            // -------------------------------------------------

            if (createdAlert != null) {

                string? componentCode = null;

                // 관련 구성요소가 있는 경우 구성요소 코드 조회
                if (createdAlert.ComponentId != null) {

                    componentCode =
                        await _context.SystemComponents
                            .Where(c =>
                                c.ComponentId
                                == createdAlert.ComponentId)
                            .Select(c => c.ComponentCode)
                            .FirstOrDefaultAsync();
                }

                await _mqttPublisher.PublishAsync(
                    "smart_sorting/alert",
                    new {
                        alertId =
                            createdAlert.AlertId,

                        alertType =
                            createdAlert.AlertType,

                        priority =
                            createdAlert.Priority,

                        componentCode =
                            componentCode,

                        // INFO 알림은 ErrorCode 없음
                        errorCode =
                            (string?)null,

                        // Qt 작업자 화면용
                        shortMessage =
                            $"초콜릿 {completedChocolateSetCount}세트 완료",

                        // Web 상세 표시용
                        alertMessage =
                            createdAlert.AlertMessage,

                        createdAt =
                            createdAlert.CreatedAt
                    }
                );
            }

            return productDetection;
        }
    }
}