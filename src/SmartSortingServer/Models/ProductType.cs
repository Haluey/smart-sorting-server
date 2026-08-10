namespace SmartSortingServer.Models {
    public class ProductType {
        // 제품 유형 고유 번호
        public long ProductTypeId { get; set; }

        // 제품 유형 코드 (CHOCOLATE / CANDY)
        public string ProductTypeCode { get; set; } = string.Empty;

        // 제품 이름
        public string ProductName { get; set; } = string.Empty;

        // 1세트당 제품 수량
        public int UnitPerSet { get; set; }

        // 등록 일시
        public DateTime CreatedAt { get; set; }
    }
}