using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartSortingServer.Data;
using SmartSortingServer.DTOs;
using SmartSortingServer.Models;
using System.Security.Claims;

namespace SmartSortingServer.Controllers {
    [ApiController]
    [Route("api/production-sessions")]
    [Authorize]
    public class ProductionSessionsController : ControllerBase {
        private readonly AppDbContext _context;

        public ProductionSessionsController(AppDbContext context) {
            _context = context;
        }

        // 생산 작업 시작
        [HttpPost("start")]
        public async Task<IActionResult> StartProduction(
            ProductionSessionStartRequest request
        ) {
            // JWT에서 로그인한 사용자 ID 가져오기
            var userIdValue = User.FindFirstValue(
                ClaimTypes.NameIdentifier
            );

            if (!long.TryParse(userIdValue, out long userId)) {
                return Unauthorized(new {
                    message = "사용자 정보를 확인할 수 없습니다."
                });
            }

            // 목표 수량 확인
            if (
                request.TargetChocolateSetCount < 0 ||
                request.TargetCandyCount < 0
            ) {
                return BadRequest(new {
                    message = "목표 수량은 0 이상이어야 합니다."
                });
            }

            // 초콜릿과 사탕 목표가 모두 0이면 생산 시작 불가
            if (
                request.TargetChocolateSetCount == 0 &&
                request.TargetCandyCount == 0
            ) {
                return BadRequest(new {
                    message = "하나 이상의 목표 수량을 입력해야 합니다."
                });
            }

            // 현재 진행 중인 생산 작업이 있는지 확인
            bool hasActiveSession = await _context.ProductionSessions
                .AnyAsync(s =>
                    s.Status == "RUNNING" ||
                    s.Status == "PAUSED"
                );

            // 이미 진행 중인 작업이 있으면 새 작업 생성 불가
            if (hasActiveSession) {
                return Conflict(new {
                    message = "이미 진행 중인 생산 작업이 있습니다."
                });
            }

            // 새로운 생산 작업 생성
            var productionSession = new ProductionSession {
                UserId = userId,

                TargetChocolateSetCount =
                    request.TargetChocolateSetCount,

                TargetCandyCount =
                    request.TargetCandyCount,

                ChocolateCount = 0,
                CandyCount = 0,

                Status = "RUNNING",

                StartedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            // DB에 생산 작업 추가
            _context.ProductionSessions.Add(productionSession);

            await _context.SaveChangesAsync();

            // 생성된 생산 작업 정보 반환
            return Ok(new {
                message = "생산 작업 시작",
                sessionId = productionSession.SessionId,
                userId = productionSession.UserId,
                targetChocolateSetCount =
                    productionSession.TargetChocolateSetCount,
                targetCandyCount =
                    productionSession.TargetCandyCount,
                status = productionSession.Status,
                startedAt = productionSession.StartedAt
            });
        }

        // 현재 로그인한 사용자의 진행 중인 생산 작업 조회
        [HttpGet("current")]
        public async Task<IActionResult> GetCurrentProduction() {
            // JWT에서 로그인한 사용자 ID 가져오기
            var userIdValue = User.FindFirstValue(
                ClaimTypes.NameIdentifier
            );

            if (!long.TryParse(userIdValue, out long userId)) {
                return Unauthorized(new {
                    message = "사용자 정보를 확인할 수 없습니다."
                });
            }

            // 현재 진행 중인 생산 작업 조회
            var productionSession = await _context.ProductionSessions
                .Where(s =>
                    s.Status == "RUNNING" ||
                    s.Status == "PAUSED"
                )
                .OrderByDescending(s => s.StartedAt)
                .FirstOrDefaultAsync();

            // 진행 중인 작업이 없는 경우
            if (productionSession == null) {
                return NotFound(new {
                    message = "진행 중인 생산 작업이 없습니다."
                });
            }

            return Ok(new {
                sessionId = productionSession.SessionId,
                userId = productionSession.UserId,
                targetChocolateSetCount =
                    productionSession.TargetChocolateSetCount,
                targetCandyCount =
                    productionSession.TargetCandyCount,
                chocolateCount = productionSession.ChocolateCount,
                candyCount = productionSession.CandyCount,
                status = productionSession.Status,
                startedAt = productionSession.StartedAt,
                updatedAt = productionSession.UpdatedAt
            });
        }

        // 현재 생산 작업 완료
        [HttpPatch("complete")]
        public async Task<IActionResult> CompleteProduction() {
            // 현재 진행 중인 생산 작업 조회
            var productionSession = await _context.ProductionSessions
                .Where(s => s.Status == "RUNNING" || s.Status == "PAUSED")
                .OrderByDescending(s => s.StartedAt)
                .FirstOrDefaultAsync();

            // 진행 중인 생산 작업이 없는 경우
            if (productionSession == null) {
                return NotFound(new {
                    message = "진행 중인 생산 작업이 없습니다."
                });
            }

            // 생산 작업 완료 처리
            productionSession.Status = "COMPLETED";
            productionSession.EndedAt = DateTime.Now;
            productionSession.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Ok(new {
                message = "생산 작업이 완료되었습니다.",
                sessionId = productionSession.SessionId,
                status = productionSession.Status,
                endedAt = productionSession.EndedAt
            });
        }
    }
}