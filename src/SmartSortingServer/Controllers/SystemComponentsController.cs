using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartSortingServer.Data;
using SmartSortingServer.DTOs;
using SmartSortingServer.Services;

namespace SmartSortingServer.Controllers {
    [ApiController]
    [Route("api/system-components")]
    [Authorize]
    public class SystemComponentsController : ControllerBase {
        private readonly AppDbContext _context;
        private readonly MqttPublisherService _mqttPublisher;
        private readonly ILogger<SystemComponentsController> _logger;

        public SystemComponentsController(
            AppDbContext context,
            MqttPublisherService mqttPublisher,
            ILogger<SystemComponentsController> logger) {

            _context = context;
            _mqttPublisher = mqttPublisher;
            _logger = logger;
        }

        // 시스템 구성요소 전체 조회
        [HttpGet]
        public async Task<IActionResult> GetSystemComponents() {
            var components = await _context.SystemComponents
                .OrderBy(c => c.ComponentId)
                .Select(c => new {
                    componentId = c.ComponentId,
                    componentCode = c.ComponentCode,
                    componentName = c.ComponentName,
                    componentType = c.ComponentType,
                    currentStatus = c.CurrentStatus,
                    statusUpdatedAt = c.StatusUpdatedAt
                })
                .ToListAsync();

            return Ok(components);
        }

        // 시스템 구성요소 상태 변경
        [HttpPatch("{componentCode}/status")]
        public async Task<IActionResult> UpdateSystemComponentStatus(
            string componentCode,
            SystemComponentStatusRequest request) {

            // 입력된 상태값을 대문자로 변환
            string status = request.Status.ToUpper();

            // 사용할 수 있는 상태인지 확인
            string[] allowedStatuses = {
                "NORMAL",
                "WARNING",
                "ERROR",
                "OFFLINE"
            };

            if (!allowedStatuses.Contains(status)) {

                _logger.LogWarning(
                    "[COMPONENT] 상태 변경 거부 - Component: {ComponentCode}, Status: {Status}, Reason: 허용되지 않은 상태",
                    componentCode,
                    status
                );

                return BadRequest(new {
                    message =
                        "상태는 NORMAL, WARNING, ERROR, OFFLINE만 사용할 수 있습니다."
                });
            }

            // 구성요소 코드로 조회
            var component = await _context.SystemComponents
                .FirstOrDefaultAsync(
                    c => c.ComponentCode == componentCode
                );

            if (component == null) {

                _logger.LogWarning(
                    "[COMPONENT] 상태 변경 실패 - Component: {ComponentCode}, Reason: 구성요소 없음",
                    componentCode
                );

                return NotFound(new {
                    message =
                        "시스템 구성요소를 찾을 수 없습니다."
                });
            }

            // 기존 상태 저장
            string previousStatus = component.CurrentStatus;

            // 상태 변경
            component.CurrentStatus = status;
            component.StatusUpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            // 실제 상태가 변경된 경우에만 MQTT Publish
            if (previousStatus != component.CurrentStatus) {

                _logger.LogInformation(
                    "[COMPONENT] 상태 변경 - Component: {ComponentCode}, {PreviousStatus} -> {CurrentStatus}",
                    component.ComponentCode,
                    previousStatus,
                    component.CurrentStatus
                );

                await _mqttPublisher.PublishAsync(
                    "smart_sorting/component/status",
                    new {
                        componentCode = component.ComponentCode,
                        status = component.CurrentStatus
                    }
                );
            }

            return Ok(new {
                message =
                    "시스템 구성요소 상태가 변경되었습니다.",

                componentId =
                    component.ComponentId,

                componentCode =
                    component.ComponentCode,

                componentName =
                    component.ComponentName,

                previousStatus =
                    previousStatus,

                currentStatus =
                    component.CurrentStatus,

                statusUpdatedAt =
                    component.StatusUpdatedAt
            });
        }
    }
}