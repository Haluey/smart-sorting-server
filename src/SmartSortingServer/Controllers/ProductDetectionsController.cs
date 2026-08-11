using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartSortingServer.Data;
using SmartSortingServer.DTOs;
using SmartSortingServer.Models;

namespace SmartSortingServer.Controllers {
    [ApiController]
    [Route("api/product-detections")]
    [Authorize]
    public class ProductDetectionsController : ControllerBase {
        private readonly AppDbContext _context;

        public ProductDetectionsController(AppDbContext context) {
            _context = context;
        }

        // 제품 감지 결과 저장
        [HttpPost]
        public async Task<IActionResult> CreateProductDetection(
            ProductDetectionRequest request) {

            // 현재 진행 중인 생산 작업 조회
            var productionSession = await _context.ProductionSessions
                .Where(s => s.Status == "RUNNING" || s.Status == "PAUSED")
                .OrderByDescending(s => s.StartedAt)
                .FirstOrDefaultAsync();

            if (productionSession == null) {
                return NotFound(new {
                    message = "진행 중인 생산 작업이 없습니다."
                });
            }

            // 분류 상태 확인
            if (request.ClassificationStatus != "SUCCESS"
                && request.ClassificationStatus != "FAILED") {

                return BadRequest(new {
                    message = "분류 상태는 SUCCESS 또는 FAILED만 사용할 수 있습니다."
                });
            }

            ProductType? productType = null;

            // 분류 성공인 경우
            if (request.ClassificationStatus == "SUCCESS") {

                if (string.IsNullOrWhiteSpace(request.ProductTypeCode)) {
                    return BadRequest(new {
                        message = "분류 성공 시 제품 유형이 필요합니다."
                    });
                }

                if (request.Confidence == null
                    || request.Confidence < 0
                    || request.Confidence > 1) {

                    return BadRequest(new {
                        message = "신뢰도는 0 이상 1 이하의 값이어야 합니다."
                    });
                }

                // 제품 코드로 제품 유형 조회
                productType = await _context.ProductTypes
                    .FirstOrDefaultAsync(
                        p => p.ProductTypeCode == request.ProductTypeCode
                    );

                if (productType == null) {
                    return BadRequest(new {
                        message = "등록되지 않은 제품 유형입니다."
                    });
                }
            }

            // 분류 실패인 경우
            if (request.ClassificationStatus == "FAILED"
                && request.ProductTypeCode != null) {

                return BadRequest(new {
                    message = "분류 실패 시 제품 유형을 지정할 수 없습니다."
                });
            }

            // 제품 감지 결과 생성
            var productDetection = new ProductDetection {
                SessionId = productionSession.SessionId,
                ProductTypeId = productType?.ProductTypeId,
                Confidence = request.Confidence,
                ImagePath = request.ImagePath,
                ClassificationStatus = request.ClassificationStatus,
                DetectedAt = DateTime.Now
            };

            // 제품 감지 결과 먼저 저장
            // ProductDetectionId를 생성하기 위해 먼저 저장
            _context.ProductDetections.Add(productDetection);
            await _context.SaveChangesAsync();

            // 분류 성공 시 생산 수량 증가
            if (request.ClassificationStatus == "SUCCESS" && productType != null) {
                if (productType.ProductTypeCode == "CHOCOLATE") {
                    productionSession.ChocolateCount += 1;

                    // 초콜릿 세트 완료 시 INFO 알림 생성
                    if (productionSession.ChocolateCount % productType.UnitPerSet == 0) {
                        int completedSetCount =
                            productionSession.ChocolateCount / productType.UnitPerSet;

                        var infoAlert = new Alert {
                            SessionId = productionSession.SessionId,
                            ComponentId = null,
                            ProductDetectionId = productDetection.ProductDetectionId,
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
                    }
                }
                else if (productType.ProductTypeCode == "CANDY") {
                    productionSession.CandyCount += 1;
                }

                productionSession.UpdatedAt = DateTime.Now;
            }

            // 분류 실패 시 ERROR 알림 생성
            if (request.ClassificationStatus == "FAILED") {

                // 비전 모듈 조회
                var visionModule = await _context.SystemComponents
                    .FirstOrDefaultAsync(
                        c => c.ComponentCode == "VISION_MODULE"
                    );

                var errorAlert = new Alert {
                    SessionId = productionSession.SessionId,
                    ComponentId = visionModule?.ComponentId,
                    ProductDetectionId = productDetection.ProductDetectionId,
                    CheckedByUserId = null,

                    AlertType = "ERROR",
                    Priority = "MEDIUM",
                    RecoveryStatus = "NOT_RECOVERED",
                    CheckStatus = "UNCHECKED",

                    AlertMessage = "제품 분류에 실패했습니다.",

                    CreatedAt = DateTime.Now,
                    RecoveredAt = null,
                    CheckedAt = null
                };

                _context.Alerts.Add(errorAlert);

                // 비전 모듈 상태도 ERROR로 변경
                if (visionModule != null) {
                    visionModule.CurrentStatus = "ERROR";
                    visionModule.StatusUpdatedAt = DateTime.Now;
                }
            }

            // 생산 수량, 알림, 장비 상태 변경 저장
            await _context.SaveChangesAsync();

            return Ok(new {
                message = "제품 감지 결과가 저장되었습니다.",
                productDetectionId = productDetection.ProductDetectionId,
                sessionId = productDetection.SessionId,
                productTypeCode = request.ProductTypeCode,
                confidence = productDetection.Confidence,
                classificationStatus = productDetection.ClassificationStatus,
                detectedAt = productDetection.DetectedAt
            });
        }
    }
}