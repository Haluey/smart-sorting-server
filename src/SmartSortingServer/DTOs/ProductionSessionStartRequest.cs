namespace SmartSortingServer.DTOs {
    public class ProductionSessionStartRequest {
        // 초콜릿 목표 세트 수
        public int TargetChocolateSetCount { get; set; }

        // 사탕 목표 수량
        public int TargetCandyCount { get; set; }
    }
}