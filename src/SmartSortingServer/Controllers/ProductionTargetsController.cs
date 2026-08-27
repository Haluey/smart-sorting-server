using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartSortingServer.Data;

namespace SmartSortingServer.Controllers {
    [ApiController]
    [Route("api/production-targets")]
    [Authorize]
    public class ProductionTargetsController : ControllerBase {

        private readonly AppDbContext _context;
        private readonly ILogger<ProductionTargetsController> _logger;

        public ProductionTargetsController(
            AppDbContext context,
            ILogger<ProductionTargetsController> logger
        ) {
            _context = context;
            _logger = logger;
        }

        // 현재 생산 목표 조회
        [HttpGet("current")]
        public async Task<IActionResult> GetCurrentTarget() {
            var target = await _context.ProductionTargets
                .FirstOrDefaultAsync(t => t.TargetId == 1);

            if (target == null) {
                return NotFound(new {
                    message = "생산 목표가 설정되어 있지 않습니다."
                });
            }

            DateTime today = DateTime.Today;
            DateTime tomorrow = today.AddDays(1);

            // 오늘 생성된 생산 세션이 있는지 확인
            bool hasTodaySession =
                await _context.ProductionSessions
                    .AnyAsync(s =>
                        s.StartedAt >= today &&
                        s.StartedAt < tomorrow
                    );

            /*
             * 오늘 세션이 아직 없고
             * 다음 목표가 예약되어 있다면
             * 예약 목표를 오늘 목표로 적용
             */
            if (!hasTodaySession &&
                target.NextTargetChocolateSetCount.HasValue &&
                target.NextTargetCandySetCount.HasValue) {

                target.TargetChocolateSetCount =
                    target.NextTargetChocolateSetCount.Value;

                target.TargetCandySetCount =
                    target.NextTargetCandySetCount.Value;

                target.NextTargetChocolateSetCount = null;
                target.NextTargetCandySetCount = null;

                target.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "[TARGET] 예약 생산 목표 적용 - ChocolateSet: {ChocolateSet}, CandySet: {CandySet}",
                    target.TargetChocolateSetCount,
                    target.TargetCandySetCount
                );
            }

            return Ok(new {
                targetChocolateSetCount =
                    target.TargetChocolateSetCount,

                targetCandySetCount =
                    target.TargetCandySetCount,

                nextTargetChocolateSetCount =
                    target.NextTargetChocolateSetCount,

                nextTargetCandySetCount =
                    target.NextTargetCandySetCount,

                dailyWorkerCount =
                    target.DailyWorkerCount,

                updatedAt =
                    target.UpdatedAt
            });
        }

        // 생산 목표 설정
        [HttpPut("current")]
        public async Task<IActionResult> UpdateCurrentTarget(
            [FromBody] UpdateProductionTargetRequest request
        ) {
            if (request.TargetChocolateSetCount <= 0 ||
                request.TargetCandySetCount <= 0) {

                return BadRequest(new {
                    message = "목표 생산량은 1 이상이어야 합니다."
                });
            }

            var target = await _context.ProductionTargets
                .FirstOrDefaultAsync(t => t.TargetId == 1);

            if (target == null) {
                return NotFound(new {
                    message = "생산 목표가 설정되어 있지 않습니다."
                });
            }

            // 목표는 작업 인원 수 이상이어야 함
            if (request.TargetChocolateSetCount < target.DailyWorkerCount ||
                request.TargetCandySetCount < target.DailyWorkerCount) {

                return BadRequest(new {
                    message =
                        $"하루 생산 목표는 작업 인원({target.DailyWorkerCount}명) 이상이어야 합니다."
                });
            }

            DateTime today = DateTime.Today;
            DateTime tomorrow = today.AddDays(1);

            // 오늘 생산 세션 존재 여부
            bool hasTodaySession =
                await _context.ProductionSessions
                    .AnyAsync(s =>
                        s.StartedAt >= today &&
                        s.StartedAt < tomorrow
                    );

            /*
             * 오늘 생산이 이미 시작되었다면
             * 현재 목표는 변경하지 않고
             * 다음 날 목표로 예약
             */
            if (hasTodaySession) {
                target.NextTargetChocolateSetCount =
                    request.TargetChocolateSetCount;

                target.NextTargetCandySetCount =
                    request.TargetCandySetCount;

                target.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "[TARGET] 다음 생산 목표 예약 - ChocolateSet: {ChocolateSet}, CandySet: {CandySet}",
                    target.NextTargetChocolateSetCount,
                    target.NextTargetCandySetCount
                );

                return Ok(new {
                    message = "다음 날 생산 목표가 설정되었습니다.",

                    targetChocolateSetCount =
                        target.TargetChocolateSetCount,

                    targetCandySetCount =
                        target.TargetCandySetCount,

                    nextTargetChocolateSetCount =
                        target.NextTargetChocolateSetCount,

                    nextTargetCandySetCount =
                        target.NextTargetCandySetCount,

                    dailyWorkerCount =
                        target.DailyWorkerCount,

                    updatedAt =
                        target.UpdatedAt
                });
            }

            /*
             * 오늘 생산이 아직 시작되지 않았다면
             * 현재 목표를 바로 변경
             */
            target.TargetChocolateSetCount =
                request.TargetChocolateSetCount;

            target.TargetCandySetCount =
                request.TargetCandySetCount;

            target.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "[TARGET] 생산 목표 변경 - ChocolateSet: {ChocolateSet}, CandySet: {CandySet}",
                target.TargetChocolateSetCount,
                target.TargetCandySetCount
            );

            return Ok(new {
                message = "오늘 생산 목표가 설정되었습니다.",

                targetChocolateSetCount =
                    target.TargetChocolateSetCount,

                targetCandySetCount =
                    target.TargetCandySetCount,

                nextTargetChocolateSetCount =
                    target.NextTargetChocolateSetCount,

                nextTargetCandySetCount =
                    target.NextTargetCandySetCount,

                dailyWorkerCount =
                    target.DailyWorkerCount,

                updatedAt =
                    target.UpdatedAt
            });
        }
    }

    public class UpdateProductionTargetRequest {
        public int TargetChocolateSetCount { get; set; }

        public int TargetCandySetCount { get; set; }
    }
}