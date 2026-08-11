namespace SmartSortingServer.DTOs {
    public class ProductDetectionRequest {
        public string? ProductTypeCode { get; set; }
        public decimal? Confidence { get; set; }
        public string? ImagePath { get; set; }
        public string ClassificationStatus { get; set; } = string.Empty;
    }
}