namespace SmartSortingServer.Models {
    public class SystemComponent {
        // 시스템 구성요소 고유 번호
        public long ComponentId { get; set; }

        // 구성요소 코드
        public string ComponentCode { get; set; } = string.Empty;

        // 구성요소 이름
        public string ComponentName { get; set; } = string.Empty;

        // 구성요소 유형
        // SENSOR / ACTUATOR / CONTROLLER / DISPLAY / SOFTWARE / SERVER / DATABASE
        public string ComponentType { get; set; } = string.Empty;

        // 현재 상태
        // NORMAL / WARNING / ERROR / OFFLINE
        public string CurrentStatus { get; set; } = string.Empty;

        // 상태가 마지막으로 변경된 일시
        public DateTime StatusUpdatedAt { get; set; }
    }
}