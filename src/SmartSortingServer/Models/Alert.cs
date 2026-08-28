namespace SmartSortingServer.Models {
    public class Alert {
        // 알림 고유 번호
        public long AlertId { get; set; }

        // 생산 작업 고유 번호
        // 생산 작업과 무관한 알림이면 NULL
        public long? SessionId { get; set; }

        // 시스템 구성요소 고유 번호
        // 특정 장비/소프트웨어와 무관한 알림이면 NULL
        public long? ComponentId { get; set; }

        // 제품 감지 고유 번호
        // 특정 제품 감지와 무관한 알림이면 NULL
        public long? ProductDetectionId { get; set; }

        // 알림을 확인한 사용자 고유 번호
        // 아직 확인하지 않았으면 NULL
        public long? CheckedByUserId { get; set; }

        // 알림 유형
        // INFO / WARNING / ERROR
        public string AlertType { get; set; } = string.Empty;

        // 중요도
        // LOW / MEDIUM / HIGH
        public string Priority { get; set; } = string.Empty;

        // 오류 식별 코드
        // Component 상태 이벤트와 무관한 알림이면 NULL
        public string? ErrorCode { get; set; }

        // 복구 상태
        // NOT_RECOVERED / RECOVERED
        // INFO 알림이면 NULL
        public string? RecoveryStatus { get; set; }

        // 확인 상태
        // UNCHECKED / CHECKED
        // INFO 알림이면 NULL
        public string? CheckStatus { get; set; }

        // 알림 상세 내용
        public string AlertMessage { get; set; } = string.Empty;

        // 알림 발생 일시
        public DateTime CreatedAt { get; set; }

        // 복구 완료 일시
        // 아직 복구되지 않았거나 INFO 알림이면 NULL
        public DateTime? RecoveredAt { get; set; }

        // 알림 확인 일시
        // 아직 확인하지 않았거나 INFO 알림이면 NULL
        public DateTime? CheckedAt { get; set; }

        // FK 관계: 생산 작업
        public ProductionSession? ProductionSession { get; set; }

        // FK 관계: 시스템 구성요소
        public SystemComponent? SystemComponent { get; set; }

        // FK 관계: 제품 감지
        public ProductDetection? ProductDetection { get; set; }

        // FK 관계: 알림을 확인한 사용자
        public User? CheckedByUser { get; set; }
    }
}