namespace SmartSortingServer.DTOs {
    public class AlertCreateRequest {
        public string ComponentCode { get; set; } = string.Empty;
        public long? ProductDetectionId { get; set; }
        public string AlertType { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;

        // Qt 작업자 화면용 짧은 메시지
        public string ShortMessage { get; set; } = string.Empty;

        // Web 상세 표시용 메시지
        public string AlertMessage { get; set; } = string.Empty;
    }
}