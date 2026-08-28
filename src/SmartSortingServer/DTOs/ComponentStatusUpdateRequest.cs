namespace SmartSortingServer.DTOs {
    public class ComponentStatusUpdateRequest {
        public string ComponentCode { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? ErrorCode { get; set; }
    }
}
