namespace SmartSortingServer.DTOs {
    public class LoginRequest {
        // 로그인 아이디
        public string LoginId { get; set; } = string.Empty;

        // 로그인 비밀번호
        public string Password { get; set; } = string.Empty;
    }
}