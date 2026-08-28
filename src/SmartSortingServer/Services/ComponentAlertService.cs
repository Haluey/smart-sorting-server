using Microsoft.EntityFrameworkCore;
using SmartSortingServer.Data;
using SmartSortingServer.Models;

namespace SmartSortingServer.Services {
    public class ComponentAlertService {
        private readonly AppDbContext _context;

        public ComponentAlertService(AppDbContext context) {
            _context = context;
        }

        public (
            string ComponentCode,
            string Status,
            string ShortMessage,
            string DetailMessage,
            string Priority
        ) GetAlertInfo(string errorCode) {

            return errorCode switch {

                "NO_DETECTION" => (
                    "CAMERA",
                    "WARNING",
                    "제품 미검출",
                    "YOLO에서 제품 객체를 검출하지 못했습니다.",
                    "MEDIUM"
                ),

                "CAMERA_ERROR" => (
                    "CAMERA",
                    "ERROR",
                    "카메라 오류",
                    "카메라 촬영에 실패했습니다.",
                    "HIGH"
                ),

                "YOLO_ERROR" => (
                    "CAMERA",
                    "ERROR",
                    "YOLO 오류",
                    "YOLO 추론에 실패하여 제품을 분류할 수 없습니다.",
                    "HIGH"
                ),

                "MODEL_LOAD_ERROR" => (
                    "CAMERA",
                    "ERROR",
                    "모델 로딩 오류",
                    "YOLO 모델을 불러오지 못했습니다.",
                    "HIGH"
                ),

                "IMAGE_SAVE_ERROR" => (
                    "CAMERA",
                    "ERROR",
                    "이미지 저장 오류",
                    "촬영한 이미지 저장에 실패했습니다.",
                    "HIGH"
                ),

                "SERVO_ACK_TIMEOUT" => (
                    "SORTING_SERVO",
                    "ERROR",
                    "서보모터 응답 없음",
                    "예상한 Servo ACK 응답을 제한 시간 내에 받지 못했습니다.",
                    "HIGH"
                ),

                "SERVO_ACK_ERROR" => (
                    "SORTING_SERVO",
                    "ERROR",
                    "서보모터 응답 오류",
                    "예상한 Servo ACK와 다른 응답을 수신했습니다.",
                    "HIGH"
                ),

                "STEPPER_ERROR" => (
                    "CONVEYOR",
                    "ERROR",
                    "컨베이어 오류",
                    "Conveyor Stepper Motor가 정상적으로 동작하지 않습니다.",
                    "HIGH"
                ),

                "IR_ERROR" => (
                    "IR_SENSOR",
                    "ERROR",
                    "IR 센서 오류",
                    "IR 센서가 정상적으로 동작하지 않습니다.",
                    "HIGH"
                ),

                "BUZZER_ERROR" => (
                    "BUZZER",
                    "ERROR",
                    "부저 오류",
                    "Buzzer가 정상적으로 동작하지 않습니다.",
                    "HIGH"
                ),

                "SERIAL_DISCONNECTED" => (
                    "ARDUINO",
                    "OFFLINE",
                    "Arduino 연결 끊김",
                    "Arduino와의 Serial 연결이 끊어졌습니다.",
                    "HIGH"
                ),

                "SERIAL_ERROR" => (
                    "ARDUINO",
                    "ERROR",
                    "Serial 통신 오류",
                    "Arduino Serial 통신 중 오류가 발생했습니다.",
                    "HIGH"
                ),

                "SERIAL_TIMEOUT" => (
                    "ARDUINO",
                    "ERROR",
                    "Serial 응답 없음",
                    "Arduino의 Serial 응답을 제한 시간 내에 받지 못했습니다.",
                    "HIGH"
                ),

                "ARDUINO_ERROR" => (
                    "ARDUINO",
                    "ERROR",
                    "Arduino 오류",
                    "Arduino가 정상적으로 동작하지 않습니다.",
                    "HIGH"
                ),

                "UNKNOWN_COMMAND" => (
                    "ARDUINO",
                    "ERROR",
                    "알 수 없는 명령",
                    "Arduino에서 알 수 없는 명령을 수신했습니다.",
                    "HIGH"
                ),

                _ => (
                    "UNKNOWN",
                    "ERROR",
                    "알 수 없는 오류",
                    "정의되지 않은 시스템 오류가 발생했습니다.",
                    "HIGH"
                )
            };
        }

        public async Task<Alert?> CreateComponentAlertAsync(
            SystemComponent component,
            string errorCode) {

            var alertInfo =
                GetAlertInfo(errorCode);

            // 이미 같은 오류가 복구되지 않은 상태로 존재하면
            // 중복 Alert를 생성하지 않음
            var existingAlert =
                await _context.Alerts
                    .FirstOrDefaultAsync(a =>
                        a.ComponentId == component.ComponentId
                        && a.ErrorCode == errorCode
                        && a.RecoveryStatus == "NOT_RECOVERED"
                    );

            if (existingAlert != null) {
                return null;
            }


            // 현재 생산 세션 조회
            var productionSession =
                await _context.ProductionSessions
                    .Where(s =>
                        s.Status == "RUNNING"
                        || s.Status == "PAUSED"
                    )
                    .OrderByDescending(s => s.StartedAt)
                    .FirstOrDefaultAsync();


            // Component 상태를 AlertType으로 변환
            string alertType =
                alertInfo.Status == "WARNING"
                    ? "WARNING"
                    : "ERROR";


            var alert = new Alert {
                SessionId =
                    productionSession?.SessionId,

                ComponentId =
                    component.ComponentId,

                ProductDetectionId =
                    null,

                CheckedByUserId =
                    null,

                AlertType =
                    alertType,

                Priority =
                    alertInfo.Priority,

                ErrorCode =
                    errorCode,

                AlertMessage =
                    alertInfo.DetailMessage,

                RecoveryStatus =
                    "NOT_RECOVERED",

                CheckStatus =
                    "UNCHECKED",

                CreatedAt =
                    DateTime.Now,

                RecoveredAt =
                    null,

                CheckedAt =
                    null
            };


            _context.Alerts.Add(alert);

            await _context.SaveChangesAsync();

            return alert;
        }

        public async Task<int> RecoverComponentAlertsAsync(
            SystemComponent component) {

            // 해당 Component의 미복구 WARNING / ERROR 알림 조회
            var unrecoveredAlerts =
                await _context.Alerts
                    .Where(a =>
                        a.ComponentId == component.ComponentId
                        && a.ErrorCode != null
                        && a.RecoveryStatus == "NOT_RECOVERED"
                    )
                    .ToListAsync();

            if (unrecoveredAlerts.Count == 0) {
                return 0;
            }

            DateTime recoveredAt =
                DateTime.Now;

            foreach (var alert in unrecoveredAlerts) {
                alert.RecoveryStatus =
                    "RECOVERED";

                alert.RecoveredAt =
                    recoveredAt;
            }

            await _context.SaveChangesAsync();

            return unrecoveredAlerts.Count;
        }

    }
}