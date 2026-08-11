namespace SmartSortingServer.DTOs {
    public class AlertCreateRequest {
        public string ComponentCode { get; set; } = string.Empty;
        public long? ProductDetectionId { get; set; }
        public string AlertType { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string AlertMessage { get; set; } = string.Empty;
    }
}