namespace SmartSortingServer.Models {
    public class ProductionSession {
        // 생산 작업 고유 번호
        public long SessionId { get; set; }

        // 작업자 고유 번호
        public long UserId { get; set; }

        // 초콜릿 목표 세트 수
        public int TargetChocolateSetCount { get; set; }

        // 사탕 목표 세트 수
        public int TargetCandySetCount { get; set; }

        // 현재 초콜릿 생산 수량
        public int ChocolateCount { get; set; }

        // 현재 사탕 생산 수량
        public int CandyCount { get; set; }

        // 생산 작업 상태
        // RUNNING / PAUSED / COMPLETED / CANCELLED
        public string Status { get; set; } = string.Empty;

        // 작업 시작 일시
        public DateTime StartedAt { get; set; }

        // 작업 종료 일시
        public DateTime? EndedAt { get; set; }

        // 마지막 수정 일시
        public DateTime UpdatedAt { get; set; }

        // FK 관계: 이 생산 작업을 수행한 사용자
        public User User { get; set; } = null!;
    }
}