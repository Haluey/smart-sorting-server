using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SmartSortingServer.Data;
using SmartSortingServer.Services;
using System.Text;

namespace SmartSortingServer {
    public class Program {
        public static void Main(string[] args) {
            var builder = WebApplication.CreateBuilder(args);

            // 서비스 등록
            builder.Services.AddControllers();

            builder.Services.AddScoped<ProductDetectionService>();
            builder.Services.AddSingleton<MqttPublisherService>();
            builder.Services.AddHostedService<MqttSubscriberService>();

            // OpenAPI 문서 기능 등록
            builder.Services.AddOpenApi();

            // MySQL 연결 문자열 가져오기
            var connectionString = builder.Configuration
                .GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException(
                    "DefaultConnection 연결 문자열을 찾을 수 없습니다."
                );

            // Entity Framework Core와 MySQL 연결 설정
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseMySQL(connectionString)
            );

            // JWT 설정값 가져오기
            var jwtKey = builder.Configuration["Jwt:Key"]
                ?? throw new InvalidOperationException(
                    "JWT Key를 찾을 수 없습니다."
                );

            var jwtIssuer = builder.Configuration["Jwt:Issuer"]
                ?? throw new InvalidOperationException(
                    "JWT Issuer를 찾을 수 없습니다."
                );

            var jwtAudience = builder.Configuration["Jwt:Audience"]
                ?? throw new InvalidOperationException(
                    "JWT Audience를 찾을 수 없습니다."
                );

            // JWT 인증 설정
            builder.Services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options => {
                    options.TokenValidationParameters = new TokenValidationParameters {
                        // 토큰 발급자 확인
                        ValidateIssuer = true,

                        // 토큰 사용 대상 확인
                        ValidateAudience = true,

                        // 토큰 만료시간 확인
                        ValidateLifetime = true,

                        // 토큰 서명 확인
                        ValidateIssuerSigningKey = true,

                        ValidIssuer = jwtIssuer,
                        ValidAudience = jwtAudience,

                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(jwtKey)
                        )
                    };
                });

            //Console.WriteLine(BCrypt.Net.BCrypt.HashPassword("1234"));

            var app = builder.Build();

            // 개발 환경에서 OpenAPI 엔드포인트 활성화
            if (app.Environment.IsDevelopment()) {
                app.MapOpenApi();
            }

            // HTTP 요청을 HTTPS로 리다이렉트
            if (!app.Environment.IsDevelopment()) {
                app.UseHttpsRedirection();
            }

            // 사용자 인증 처리
            app.UseAuthentication();

            // 사용자 권한 처리
            app.UseAuthorization();

            // Controller API 엔드포인트 연결
            app.MapControllers();

            // 애플리케이션 실행
            app.Run();
        }
    }
}