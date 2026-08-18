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

        public ProductionTargetsController(AppDbContext context) {
            _context = context;
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

            return Ok(new {
                targetChocolateSetCount = target.TargetChocolateSetCount,
                targetCandyCount = target.TargetCandyCount,
                updatedAt = target.UpdatedAt
            });
        }

        // 생산 목표 설정
        [HttpPut("current")]
        public async Task<IActionResult> UpdateCurrentTarget(
            [FromBody] UpdateProductionTargetRequest request
        ) {
            if (request.TargetChocolateSetCount < 0 || request.TargetCandyCount < 0) {
                return BadRequest(new {
                    message = "목표 생산량은 0 이상이어야 합니다."
                });
            }

            var target = await _context.ProductionTargets
                .FirstOrDefaultAsync(t => t.TargetId == 1);

            if (target == null) {
                return NotFound(new {
                    message = "생산 목표가 설정되어 있지 않습니다."
                });
            }

            target.TargetChocolateSetCount = request.TargetChocolateSetCount;
            target.TargetCandyCount = request.TargetCandyCount;
            target.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Ok(new {
                message = "생산 목표가 설정되었습니다.",
                targetChocolateSetCount = target.TargetChocolateSetCount,
                targetCandyCount = target.TargetCandyCount,
                updatedAt = target.UpdatedAt
            });
        }
    }

    public class UpdateProductionTargetRequest {
        public int TargetChocolateSetCount { get; set; }
        public int TargetCandyCount { get; set; }
    }
}