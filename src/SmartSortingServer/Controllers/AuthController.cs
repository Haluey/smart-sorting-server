using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SmartSortingServer.Data;
using SmartSortingServer.DTOs;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

namespace SmartSortingServer.Controllers {
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            AppDbContext context,
            IConfiguration configuration,
            ILogger<AuthController> logger
        ) {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        // 로그인
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request) {
            // 입력한 로그인 아이디로 사용자 조회
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.LoginId == request.LoginId);

            // 사용자가 존재하지 않는 경우
            if (user == null) {
                return Unauthorized(new {
                    message = "아이디 또는 비밀번호가 올바르지 않습니다."
                });
            }

            // 입력한 비밀번호와 저장된 비밀번호 해시 비교
            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(
                request.Password,
                user.PasswordHash
            );

            // 비밀번호가 일치하지 않는 경우
            if (!isPasswordValid) {
                return Unauthorized(new {
                    message = "아이디 또는 비밀번호가 올바르지 않습니다."
                });
            }

            // JWT 설정값 가져오기
            var jwtKey = _configuration["Jwt:Key"]
                ?? throw new InvalidOperationException("JWT Key를 찾을 수 없습니다.");

            var jwtIssuer = _configuration["Jwt:Issuer"]
                ?? throw new InvalidOperationException("JWT Issuer를 찾을 수 없습니다.");

            var jwtAudience = _configuration["Jwt:Audience"]
                ?? throw new InvalidOperationException("JWT Audience를 찾을 수 없습니다.");

            // JWT에 저장할 사용자 정보
            var claims = new Dictionary<string, object> {
                [ClaimTypes.NameIdentifier] = user.UserId.ToString(),
                [ClaimTypes.Name] = user.LoginId,
                [ClaimTypes.Role] = user.Role
            };

            // JWT 서명키 생성
            var securityKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey)
            );

            var signingCredentials = new SigningCredentials(
                securityKey,
                SecurityAlgorithms.HmacSha256
            );

            // JWT 생성 설정
            var tokenDescriptor = new SecurityTokenDescriptor {
                Issuer = jwtIssuer,
                Audience = jwtAudience,
                Claims = claims,

                // 토큰 유효시간: 1시간
                Expires = DateTime.UtcNow.AddHours(1),

                SigningCredentials = signingCredentials
            };

            // JWT 문자열 생성
            var tokenHandler = new JsonWebTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            // 로그인 성공 로그
            _logger.LogInformation(
                "[LOGIN] {LoginId} 로그인 성공",
                user.LoginId
            );

            // 로그인 성공
            return Ok(new {
                message = "로그인 성공",
                token = token,
                userId = user.UserId,
                loginId = user.LoginId,
                name = user.Name,
                role = user.Role
            });
        }

        // JWT 인증 테스트
        [Authorize]
        [HttpGet("test")]
        public IActionResult TestAuth() {
            return Ok(new {
                message = "JWT 인증 성공"
            });
        }
    }
}