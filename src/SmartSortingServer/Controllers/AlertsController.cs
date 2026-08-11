using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartSortingServer.Data;
using SmartSortingServer.DTOs;
using SmartSortingServer.Models;

namespace SmartSortingServer.Controllers {
    [ApiController]
    [Route("api/alerts")]
    [Authorize]
    public class AlertsController : ControllerBase {
        private readonly AppDbContext _context;

        public AlertsController(AppDbContext context) {
            _context = context;
        }

        // 알림 전체 조회
        [HttpGet]
        public async Task<IActionResult> GetAlerts() {
            var alerts = await _context.Alerts
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => new {
                    alertId = a.AlertId,
                    sessionId = a.SessionId,
                    componentId = a.ComponentId,
                    productDetectionId = a.ProductDetectionId,
                    checkedByUserId = a.CheckedByUserId,
                    alertType = a.AlertType,
                    priority = a.Priority,
                    recoveryStatus = a.RecoveryStatus,
                    checkStatus = a.CheckStatus,
                    alertMessage = a.AlertMessage,
                    createdAt = a.CreatedAt,
                    recoveredAt = a.RecoveredAt,
                    checkedAt = a.CheckedAt
                })
                .ToListAsync();

            return Ok(alerts);
        }

        // 알림 생성
        [HttpPost]
        public async Task<IActionResult> CreateAlert(
            AlertCreateRequest request) {

            // 알림 유형 확인
            string alertType = request.AlertType.ToUpper();

            if (alertType != "INFO"
                && alertType != "WARNING"
                && alertType != "ERROR") {

                return BadRequest(new {
                    message = "알림 유형은 INFO, WARNING, ERROR만 사용할 수 있습니다."
                });
            }

            // 우선순위 확인
            string priority = request.Priority.ToUpper();

            if (priority != "LOW"
                && priority != "MEDIUM"
                && priority != "HIGH") {

                return BadRequest(new {
                    message = "우선순위는 LOW, MEDIUM, HIGH만 사용할 수 있습니다."
                });
            }

            // 알림 유형과 우선순위 조합 확인
            bool isValidTypePriority =
                (alertType == "INFO" && priority == "LOW")
                || (alertType == "WARNING"
                    && (priority == "MEDIUM" || priority == "HIGH"))
                || (alertType == "ERROR"
                    && (priority == "LOW"
                        || priority == "MEDIUM"
                        || priority == "HIGH"));

            if (!isValidTypePriority) {
                return BadRequest(new {
                    message = "알림 유형과 우선순위 조합이 올바르지 않습니다."
                });
            }

            // 알림 메시지 확인
            if (string.IsNullOrWhiteSpace(request.AlertMessage)) {
                return BadRequest(new {
                    message = "알림 메시지가 필요합니다."
                });
            }

            if (request.AlertMessage.Length > 1000) {
                return BadRequest(new {
                    message = "알림 메시지는 1000자 이하로 입력해야 합니다."
                });
            }

            // 시스템 구성요소 조회
            var component = await _context.SystemComponents
                .FirstOrDefaultAsync(
                    c => c.ComponentCode == request.ComponentCode
                );

            if (component == null) {
                return NotFound(new {
                    message = "시스템 구성요소를 찾을 수 없습니다."
                });
            }

            // 제품 감지 결과 확인
            ProductDetection? productDetection = null;

            if (request.ProductDetectionId != null) {
                productDetection = await _context.ProductDetections
                    .FirstOrDefaultAsync(
                        p => p.ProductDetectionId
                            == request.ProductDetectionId
                    );

                if (productDetection == null) {
                    return BadRequest(new {
                        message = "존재하지 않는 제품 감지 결과입니다."
                    });
                }
            }

            long? sessionId;

            // 특정 제품 감지와 연결된 알림인 경우
            if (productDetection != null) {
                // 해당 제품 감지가 속한 생산 세션 사용
                sessionId = productDetection.SessionId;
            }
            else {
                // 일반 장비 알림인 경우 현재 생산 세션 조회
                var productionSession =
                    await _context.ProductionSessions
                        .Where(s =>
                            s.Status == "RUNNING"
                            || s.Status == "PAUSED")
                        .OrderByDescending(s => s.StartedAt)
                        .FirstOrDefaultAsync();

                sessionId = productionSession?.SessionId;
            }

            // 알림 생성
            var alert = new Alert {
                SessionId = sessionId,
                ComponentId = component.ComponentId,

                ProductDetectionId =
                    productDetection?.ProductDetectionId,

                CheckedByUserId = null,

                AlertType = alertType,
                Priority = priority,
                AlertMessage = request.AlertMessage,

                CreatedAt = DateTime.Now
            };

            // INFO 알림
            if (alertType == "INFO") {
                alert.RecoveryStatus = null;
                alert.CheckStatus = null;
                alert.RecoveredAt = null;
                alert.CheckedAt = null;
            }

            // WARNING / ERROR 알림
            else {
                alert.RecoveryStatus = "NOT_RECOVERED";
                alert.CheckStatus = "UNCHECKED";
                alert.RecoveredAt = null;
                alert.CheckedAt = null;
            }

            // WARNING인 경우 구성요소 상태 변경
            if (alertType == "WARNING") {
                component.CurrentStatus = "WARNING";
                component.StatusUpdatedAt = DateTime.Now;
            }

            // ERROR인 경우 구성요소 상태 변경
            else if (alertType == "ERROR") {
                component.CurrentStatus = "ERROR";
                component.StatusUpdatedAt = DateTime.Now;
            }

            // INFO는 구성요소 상태를 변경하지 않음

            _context.Alerts.Add(alert);

            await _context.SaveChangesAsync();

            return Ok(new {
                message = "알림이 저장되었습니다.",
                alertId = alert.AlertId,
                sessionId = alert.SessionId,
                componentCode = component.ComponentCode,
                componentStatus = component.CurrentStatus,
                productDetectionId = alert.ProductDetectionId,
                alertType = alert.AlertType,
                priority = alert.Priority,
                alertMessage = alert.AlertMessage,
                createdAt = alert.CreatedAt
            });
        }

        // 알림 확인 처리
        [HttpPatch("{alertId:long}/check")]
        public async Task<IActionResult> CheckAlert(long alertId) {

            // JWT에서 사용자 ID 조회
            string? userIdClaim = User.FindFirstValue(
                ClaimTypes.NameIdentifier
            );

            if (userIdClaim == null
                || !long.TryParse(userIdClaim, out long userId)) {

                return Unauthorized(new {
                    message = "사용자 정보를 확인할 수 없습니다."
                });
            }

            // 알림 조회
            var alert = await _context.Alerts
                .FirstOrDefaultAsync(
                    a => a.AlertId == alertId
                );

            if (alert == null) {
                return NotFound(new {
                    message = "알림을 찾을 수 없습니다."
                });
            }

            // INFO 알림은 확인 처리 대상이 아님
            if (alert.AlertType == "INFO") {
                return BadRequest(new {
                    message = "INFO 알림은 확인 처리 대상이 아닙니다."
                });
            }

            // 이미 확인된 알림
            if (alert.CheckStatus == "CHECKED") {
                return Conflict(new {
                    message = "이미 확인된 알림입니다."
                });
            }

            // 알림 확인 처리
            alert.CheckStatus = "CHECKED";
            alert.CheckedByUserId = userId;
            alert.CheckedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Ok(new {
                message = "알림이 확인 처리되었습니다.",
                alertId = alert.AlertId,
                checkStatus = alert.CheckStatus,
                checkedByUserId = alert.CheckedByUserId,
                checkedAt = alert.CheckedAt
            });
        }

        // 알림 복구 처리
        [HttpPatch("{alertId:long}/recover")]
        public async Task<IActionResult> RecoverAlert(long alertId) {

            // 알림 조회
            var alert = await _context.Alerts
                .FirstOrDefaultAsync(
                    a => a.AlertId == alertId
                );

            if (alert == null) {
                return NotFound(new {
                    message = "알림을 찾을 수 없습니다."
                });
            }

            // INFO 알림은 복구 처리 대상이 아님
            if (alert.AlertType == "INFO") {
                return BadRequest(new {
                    message = "INFO 알림은 복구 처리 대상이 아닙니다."
                });
            }

            // 이미 복구된 알림
            if (alert.RecoveryStatus == "RECOVERED") {
                return Conflict(new {
                    message = "이미 복구된 알림입니다."
                });
            }

            // 알림 복구
            alert.RecoveryStatus = "RECOVERED";
            alert.RecoveredAt = DateTime.Now;

            // 연결된 시스템 구성요소가 있는 경우
            if (alert.ComponentId != null) {

                var component = await _context.SystemComponents
                    .FirstOrDefaultAsync(
                        c => c.ComponentId == alert.ComponentId
                    );

                if (component != null) {

                    // 같은 장비에 아직 복구되지 않은 ERROR가 있는지 확인
                    bool hasUnrecoveredError =
                        await _context.Alerts.AnyAsync(a =>
                            a.AlertId != alert.AlertId
                            && a.ComponentId == alert.ComponentId
                            && a.AlertType == "ERROR"
                            && a.RecoveryStatus == "NOT_RECOVERED"
                        );

                    // 같은 장비에 아직 복구되지 않은 WARNING이 있는지 확인
                    bool hasUnrecoveredWarning =
                        await _context.Alerts.AnyAsync(a =>
                            a.AlertId != alert.AlertId
                            && a.ComponentId == alert.ComponentId
                            && a.AlertType == "WARNING"
                            && a.RecoveryStatus == "NOT_RECOVERED"
                        );

                    if (hasUnrecoveredError) {
                        component.CurrentStatus = "ERROR";
                    }
                    else if (hasUnrecoveredWarning) {
                        component.CurrentStatus = "WARNING";
                    }
                    else {
                        component.CurrentStatus = "NORMAL";
                    }

                    component.StatusUpdatedAt = DateTime.Now;
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new {
                message = "알림이 복구 처리되었습니다.",
                alertId = alert.AlertId,
                recoveryStatus = alert.RecoveryStatus,
                recoveredAt = alert.RecoveredAt
            });
        }
    }
}