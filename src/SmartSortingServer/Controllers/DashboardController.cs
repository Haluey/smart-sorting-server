using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartSortingServer.Data;

namespace SmartSortingServer.Controllers {

    [ApiController]
    [Route("api/dashboard")]
    [Authorize]
    public class DashboardController : ControllerBase {

        private readonly AppDbContext _context;

        public DashboardController(AppDbContext context) {
            _context = context;
        }

        // 오늘 생산량 요약
        [HttpGet("summary")]
        public async Task<IActionResult> GetTodaySummary() {

            DateTime today = DateTime.Today;
            DateTime tomorrow = today.AddDays(1);

            // 오늘 생산 세션 조회
            var sessions = await _context.ProductionSessions
                .Where(s =>
                    s.StartedAt >= today
                    && s.StartedAt < tomorrow
                )
                .ToListAsync();

            // 오늘 제품 감지 결과 조회
            var detections = await _context.ProductDetections
                .Where(p =>
                    p.DetectedAt >= today
                    && p.DetectedAt < tomorrow
                )
                .ToListAsync();

            // 오늘 알림 조회
            var alerts = await _context.Alerts
                .Where(a =>
                    a.CreatedAt >= today
                    && a.CreatedAt < tomorrow
                )
                .ToListAsync();

            // 초콜릿 제품 유형 조회
            var chocolateType =
                await _context.ProductTypes
                    .FirstOrDefaultAsync(
                        p => p.ProductTypeCode == "CHOCOLATE"
                    );

            // 사탕 제품 유형 조회
            var candyType =
                await _context.ProductTypes
                    .FirstOrDefaultAsync(
                        p => p.ProductTypeCode == "CANDY"
                    );

            if (chocolateType == null || candyType == null) {
                return BadRequest(new {
                    message = "제품 유형 정보를 찾을 수 없습니다."
                });
            }

            int chocolateCount =
                sessions.Sum(s => s.ChocolateCount);

            int candyCount =
                sessions.Sum(s => s.CandyCount);

            int chocolateSetCount =
                chocolateType.UnitPerSet > 0
                    ? chocolateCount / chocolateType.UnitPerSet
                    : 0;

            int candySetCount =
                candyType.UnitPerSet > 0
                    ? candyCount / candyType.UnitPerSet
                    : 0;

            // 현재 하루 생산 목표 조회
            var target = await _context.ProductionTargets
                .FirstOrDefaultAsync(t => t.TargetId == 1);

            if (target == null) {
                return BadRequest(new {
                    message = "생산 목표가 설정되어 있지 않습니다."
                });
            }

            int chocolateProgress =
                target.TargetChocolateSetCount > 0
                    ? (int)Math.Round(
                        (double)chocolateSetCount
                        / target.TargetChocolateSetCount
                        * 100
                    )
                    : 0;

            int candyProgress =
                target.TargetCandySetCount > 0
                    ? (int)Math.Round(
                        (double)candySetCount
                        / target.TargetCandySetCount
                        * 100
                    )
                    : 0;

            int successCount =
                detections.Count(
                    p => p.ClassificationStatus == "SUCCESS"
                );

            int failedCount =
                detections.Count(
                    p => p.ClassificationStatus == "FAILED"
                );

            int activeAlertCount =
                alerts.Count(a =>
                    a.AlertType != "INFO"
                    && a.RecoveryStatus == "NOT_RECOVERED"
                );

            return Ok(new {
                chocolateCount,
                chocolateSetCount,
                targetChocolateSetCount =
                    target.TargetChocolateSetCount,
                chocolateProgress,

                candyCount,
                candySetCount,
                targetCandySetCount =
                    target.TargetCandySetCount,
                candyProgress,

                successCount,
                failedCount,
                activeAlertCount
            });
        }

        // 오늘 시간대별 생산량 추이
        [HttpGet("hourly-production")]
        public async Task<IActionResult> GetHourlyProduction() {

            DateTime today = DateTime.Today;
            DateTime tomorrow = today.AddDays(1);

            var detections = await _context.ProductDetections
                .Where(p =>
                    p.DetectedAt >= today
                    && p.DetectedAt < tomorrow
                    && p.ClassificationStatus == "SUCCESS"
                )
                .ToListAsync();

            var productTypes = await _context.ProductTypes
                .ToDictionaryAsync(
                    p => p.ProductTypeId,
                    p => p.ProductTypeCode
                );

            var result = Enumerable.Range(0, 24)
                .Select(hour => {

                    var hourlyDetections =
                        detections.Where(
                            p => p.DetectedAt.Hour == hour
                        );

                    int chocolateCount =
                        hourlyDetections.Count(p =>
                            p.ProductTypeId != null
                            && productTypes.ContainsKey(
                                p.ProductTypeId.Value
                            )
                            && productTypes[p.ProductTypeId.Value]
                                == "CHOCOLATE"
                        );

                    int candyCount =
                        hourlyDetections.Count(p =>
                            p.ProductTypeId != null
                            && productTypes.ContainsKey(
                                p.ProductTypeId.Value
                            )
                            && productTypes[p.ProductTypeId.Value]
                                == "CANDY"
                        );

                    return new {
                        hour,
                        chocolateCount,
                        candyCount
                    };
                })
                .ToList();

            return Ok(result);
        }

        // 오늘 제품 종류별 분류 비율
        [HttpGet("classification-ratio")]
        public async Task<IActionResult> GetClassificationRatio() {

            DateTime today = DateTime.Today;
            DateTime tomorrow = today.AddDays(1);

            // 오늘 정상 분류된 제품만 조회
            var detections = await _context.ProductDetections
                .Where(p =>
                    p.DetectedAt >= today
                    && p.DetectedAt < tomorrow
                    && p.ClassificationStatus == "SUCCESS"
                    && p.ProductTypeId != null
                )
                .ToListAsync();

            // 제품 유형 조회
            var productTypes = await _context.ProductTypes
                .ToDictionaryAsync(
                    p => p.ProductTypeId,
                    p => p.ProductTypeCode
                );

            // 초콜릿 분류 수
            int chocolateCount =
                detections.Count(p =>
                    p.ProductTypeId != null
                    && productTypes.ContainsKey(p.ProductTypeId.Value)
                    && productTypes[p.ProductTypeId.Value] == "CHOCOLATE"
                );

            // 사탕 분류 수
            int candyCount =
                detections.Count(p =>
                    p.ProductTypeId != null
                    && productTypes.ContainsKey(p.ProductTypeId.Value)
                    && productTypes[p.ProductTypeId.Value] == "CANDY"
                );

            int totalCount =
                chocolateCount + candyCount;

            double chocolateRate =
                totalCount > 0
                    ? Math.Round(
                        (double)chocolateCount / totalCount * 100,
                        1
                    )
                    : 0;

            double candyRate =
                totalCount > 0
                    ? Math.Round(
                        (double)candyCount / totalCount * 100,
                        1
                    )
                    : 0;

            return Ok(new {
                totalCount,
                chocolateCount,
                candyCount,
                chocolateRate,
                candyRate
            });
        }

        // 최근 제품 감지 결과 조회
        [HttpGet("recent-detections")]
        public async Task<IActionResult> GetRecentDetections() {

            var detections = await _context.ProductDetections
                .OrderByDescending(p => p.DetectedAt)
                .Take(5)
                .Select(p => new {
                    productDetectionId =
                        p.ProductDetectionId,

                    sessionId =
                        p.SessionId,

                    productTypeCode =
                        p.ProductTypeId == null
                            ? null
                            : _context.ProductTypes
                                .Where(t =>
                                    t.ProductTypeId
                                    == p.ProductTypeId
                                )
                                .Select(t =>
                                    t.ProductTypeCode
                                )
                                .FirstOrDefault(),

                    confidence =
                        p.Confidence,

                    classificationStatus =
                        p.ClassificationStatus,

                    imagePath =
                        p.ImagePath,

                    detectedAt =
                        p.DetectedAt
                })
                .ToListAsync();

            return Ok(detections);
        }
    }
}