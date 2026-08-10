namespace SmartSortingServer.Models {
    public class User {
        // 사용자 고유 번호
        public long UserId { get; set; }

        // 로그인 아이디
        public string LoginId { get; set; } = string.Empty;

        // 암호화된 비밀번호
        public string PasswordHash { get; set; } = string.Empty;

        // 사용자 이름
        public string Name { get; set; } = string.Empty;

        // 사용자 권한 (ADMIN / WORKER)
        public string Role { get; set; } = string.Empty;

        // 계정 생성 일시
        public DateTime CreatedAt { get; set; }
    }
}