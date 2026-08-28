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
             * 오늘 세션이 아직 없다면
             * 예약된 생산 목표와 작업 인원을 현재 설정으로 적용
             */
            if (!hasTodaySession) {

                bool targetChanged = false;

                // 예약 생산 목표 적용
                if (target.NextTargetChocolateSetCount.HasValue &&
                    target.NextTargetCandySetCount.HasValue) {

                    target.TargetChocolateSetCount =
                        target.NextTargetChocolateSetCount.Value;

                    target.TargetCandySetCount =
                        target.NextTargetCandySetCount.Value;

                    target.NextTargetChocolateSetCount = null;
                    target.NextTargetCandySetCount = null;

                    targetChanged = true;

                    _logger.LogInformation(
                        "[TARGET] 예약 생산 목표 적용 - ChocolateSet: {ChocolateSet}, CandySet: {CandySet}",
                        target.TargetChocolateSetCount,
                        target.TargetCandySetCount
                    );
                }

                // 예약 작업 인원 적용
                if (target.NextDailyWorkerCount.HasValue) {

                    target.DailyWorkerCount =
                        target.NextDailyWorkerCount.Value;

                    target.NextDailyWorkerCount = null;

                    targetChanged = true;

                    _logger.LogInformation(
                        "[TARGET] 예약 작업 인원 적용 - DailyWorkerCount: {DailyWorkerCount}",
                        target.DailyWorkerCount
                    );
                }

                // 실제 변경된 값이 있을 때만 DB 저장
                if (targetChanged) {
                    target.UpdatedAt = DateTime.Now;

                    await _context.SaveChangesAsync();
                }
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

                nextDailyWorkerCount =
                    target.NextDailyWorkerCount,

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

            DateTime today = DateTime.Today;
            DateTime tomorrow = today.AddDays(1);

            // 오늘 생산 세션 존재 여부
            bool hasTodaySession =
                await _context.ProductionSessions
                    .AnyAsync(s =>
                        s.StartedAt >= today &&
                        s.StartedAt < tomorrow
                    );

            // 오늘 생산이 이미 시작된 경우
            // 예약 작업 인원이 있으면 예약 인원을 기준으로 다음 목표 검사
            if (hasTodaySession) {

                int nextWorkerCount =
                    target.NextDailyWorkerCount
                    ?? target.DailyWorkerCount;

                if (request.TargetChocolateSetCount < nextWorkerCount ||
                    request.TargetCandySetCount < nextWorkerCount) {

                    return BadRequest(new {
                        message =
                            $"다음 생산 목표는 예약 작업 인원({nextWorkerCount}명) 이상이어야 합니다."
                    });
                }
            }
            else {
                // 오늘 생산이 아직 시작되지 않은 경우
                // 현재 작업 인원을 기준으로 현재 목표 검사
                if (request.TargetChocolateSetCount < target.DailyWorkerCount ||
                    request.TargetCandySetCount < target.DailyWorkerCount) {

                    return BadRequest(new {
                        message =
                            $"하루 생산 목표는 작업 인원({target.DailyWorkerCount}명) 이상이어야 합니다."
                    });
                }
            }

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

                updatedAt =
                    target.UpdatedAt
            });
        }

        /// 하루 작업 인원 설정
        [HttpPut("worker-count")]
        public async Task<IActionResult> UpdateDailyWorkerCount(
            [FromBody] UpdateDailyWorkerCountRequest request
        ) {
            if (request.DailyWorkerCount <= 0) {
                return BadRequest(new {
                    message = "작업 인원 수는 1명 이상이어야 합니다."
                });
            }

            var target = await _context.ProductionTargets
                .FirstOrDefaultAsync(t => t.TargetId == 1);

            if (target == null) {
                return NotFound(new {
                    message = "생산 목표 정보가 없습니다."
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
             * 현재 작업 인원은 변경하지 않고
             * 다음 날 작업 인원으로 예약
             */
            if (hasTodaySession) {

                // 다음 날 생산 목표가 예약되어 있으면 예약 목표 기준으로 검사
                int nextChocolateTarget =
                    target.NextTargetChocolateSetCount
                    ?? target.TargetChocolateSetCount;

                int nextCandyTarget =
                    target.NextTargetCandySetCount
                    ?? target.TargetCandySetCount;

                if (request.DailyWorkerCount > nextChocolateTarget ||
                    request.DailyWorkerCount > nextCandyTarget) {

                    return BadRequest(new {
                        message =
                            "작업 인원 수는 다음 생산 목표보다 많을 수 없습니다."
                    });
                }

                target.NextDailyWorkerCount =
                    request.DailyWorkerCount;

                target.UpdatedAt =
                    DateTime.Now;

                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "[TARGET] 다음 작업 인원 예약 - DailyWorkerCount: {DailyWorkerCount}",
                    target.NextDailyWorkerCount
                );

                return Ok(new {
                    message = "다음 날 작업 인원 수가 설정되었습니다.",

                    dailyWorkerCount =
                        target.DailyWorkerCount,

                    nextDailyWorkerCount =
                        target.NextDailyWorkerCount,

                    updatedAt =
                        target.UpdatedAt
                });
            }

            /*
             * 오늘 생산이 아직 시작되지 않았다면
             * 현재 작업 인원을 바로 변경
             */
            if (request.DailyWorkerCount > target.TargetChocolateSetCount ||
                request.DailyWorkerCount > target.TargetCandySetCount) {

                return BadRequest(new {
                    message =
                        "작업 인원 수는 하루 생산 목표보다 많을 수 없습니다."
                });
            }

            target.DailyWorkerCount =
                request.DailyWorkerCount;

            target.UpdatedAt =
                DateTime.Now;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "[TARGET] 하루 작업 인원 변경 - DailyWorkerCount: {DailyWorkerCount}",
                target.DailyWorkerCount
            );

            return Ok(new {
                message = "오늘 작업 인원 수가 설정되었습니다.",

                dailyWorkerCount =
                    target.DailyWorkerCount,

                nextDailyWorkerCount =
                    target.NextDailyWorkerCount,

                updatedAt =
                    target.UpdatedAt
            });
        }
    }

    public class UpdateProductionTargetRequest {
        public int TargetChocolateSetCount { get; set; }

        public int TargetCandySetCount { get; set; }
    }

    public class UpdateDailyWorkerCountRequest {
        public int DailyWorkerCount { get; set; }
    }
}