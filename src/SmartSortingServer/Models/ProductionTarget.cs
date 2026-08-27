namespace SmartSortingServer.Models {
    public class ProductionTarget {
        // 생산 목표 고유 번호
        public int TargetId { get; set; }

        // 초콜릿 목표 세트 수
        public int TargetChocolateSetCount { get; set; }

        // 사탕 목표 세트 수
        public int TargetCandySetCount { get; set; }

        // 마지막 수정 일시
        public DateTime UpdatedAt { get; set; }
    }
}