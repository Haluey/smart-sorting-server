namespace SmartSortingServer.Models {
    public class ProductDetection {
        // 제품 감지 고유 번호
        public long ProductDetectionId { get; set; }

        // 생산 작업 고유 번호
        public long SessionId { get; set; }

        // 제품 유형 고유 번호
        // 분류 실패 시 NULL
        public long? ProductTypeId { get; set; }

        // 분류 신뢰도
        // 분류 실패 시 NULL
        public decimal? Confidence { get; set; }

        // 촬영 이미지 경로
        // 촬영 실패 시 NULL
        public string? ImagePath { get; set; }

        // 분류 상태
        // SUCCESS / FAILED
        public string ClassificationStatus { get; set; } = string.Empty;

        // 감지 일시
        public DateTime DetectedAt { get; set; }

        // FK 관계: 생산 작업
        public ProductionSession ProductionSession { get; set; } = null!;

        // FK 관계: 제품 유형
        // 분류 실패 시 연결되는 제품 유형이 없으므로 NULL 가능
        public ProductType? ProductType { get; set; }
    }
}