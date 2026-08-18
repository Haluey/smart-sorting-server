using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartSortingServer.Data;
using SmartSortingServer.Models;
using SmartSortingServer.Services;
using System.Security.Claims;

namespace SmartSortingServer.Controllers {
    [ApiController]
    [Route("api/production-sessions")]
    [Authorize]
    public class ProductionSessionsController : ControllerBase {
        private readonly AppDbContext _context;
        private readonly MqttPublisherService _mqttPublisher;

        public ProductionSessionsController (AppDbContext context, MqttPublisherService mqttPublisher) {
            _context = context;
            _mqttPublisher = mqttPublisher;
        }

        // 생산 작업 시작
        [HttpPost("start")]
        public async Task<IActionResult> StartProduction() {
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
            var runningSession = await _context.ProductionSessions
                .Where(s =>
                    s.Status == "RUNNING" ||
                    s.Status == "PAUSED"
                )
                .OrderByDescending(s => s.StartedAt)
                .FirstOrDefaultAsync();

            if (runningSession != null) {
                return BadRequest(new {
                    message = "이미 진행 중인 생산 작업이 있습니다."
                });
            }

            // 현재 생산 목표 조회
            var target = await _context.ProductionTargets
                .FirstOrDefaultAsync(t => t.TargetId == 1);

            if (target == null) {
                return BadRequest(new {
                    message = "생산 목표가 설정되어 있지 않습니다."
                });
            }

            // 생산 목표 유효성 확인
            if (target.TargetChocolateSetCount <= 0 ||
                target.TargetCandyCount <= 0) {
                return BadRequest(new {
                    message = "생산 목표를 확인해주세요."
                });
            }

            // 제품 유형 조회
            var chocolateType = await _context.ProductTypes
                .FirstOrDefaultAsync(
                    p => p.ProductTypeCode == "CHOCOLATE"
                );

            var candyType = await _context.ProductTypes
                .FirstOrDefaultAsync(
                    p => p.ProductTypeCode == "CANDY"
                );

            if (chocolateType == null || candyType == null) {
                return BadRequest(new {
                    message = "제품 유형 정보를 찾을 수 없습니다."
                });
            }

            // 새로운 생산 작업 생성
            var productionSession = new ProductionSession {
                UserId = userId,
                TargetChocolateSetCount =
                    target.TargetChocolateSetCount,
                TargetCandyCount =
                    target.TargetCandyCount,
                ChocolateCount = 0,
                CandyCount = 0,
                Status = "RUNNING",
                StartedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _context.ProductionSessions.Add(productionSession);

            await _context.SaveChangesAsync();

            // 초콜릿 목표 낱개 수
            int targetChocolateCount =
                productionSession.TargetChocolateSetCount
                * chocolateType.UnitPerSet;

            // 생산 시작 MQTT Publish
            await _mqttPublisher.PublishAsync(
                "smart_sorting/production/status",
                new {
                    sessionId = productionSession.SessionId,
                    status = productionSession.Status,

                    chocolate = new {
                        currentCount =
                            productionSession.ChocolateCount,

                        targetCount =
                            targetChocolateCount,

                        unitPerSet =
                            chocolateType.UnitPerSet,

                        setCount = 0,

                        progress = 0
                    },

                    candy = new {
                        currentCount =
                            productionSession.CandyCount,

                        targetCount =
                            productionSession.TargetCandyCount,

                        unitPerSet =
                            candyType.UnitPerSet,

                        setCount = 0,

                        progress = 0
                    }
                }
            );

            return Ok(new {
                message = "생산 작업 시작",
                sessionId = productionSession.SessionId,
                userId = productionSession.UserId,
                targetChocolateSetCount =
                    productionSession.TargetChocolateSetCount,
                targetCandyCount =
                    productionSession.TargetCandyCount,
                chocolateCount =
                    productionSession.ChocolateCount,
                candyCount =
                    productionSession.CandyCount,
                status = productionSession.Status,
                startedAt =
                    productionSession.StartedAt
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

        // 현재 생산 작업 종료
        [HttpPatch("finish")]
        public async Task<IActionResult> FinishProduction() {

            // 현재 진행 중인 생산 작업 조회
            var productionSession =
                await _context.ProductionSessions
                    .Where(s =>
                        s.Status == "RUNNING"
                        || s.Status == "PAUSED"
                    )
                    .OrderByDescending(s => s.StartedAt)
                    .FirstOrDefaultAsync();

            if (productionSession == null) {
                return NotFound(new {
                    message = "진행 중인 생산 작업이 없습니다."
                });
            }

            // 제품 유형 조회
            var chocolateType = await _context.ProductTypes
                .FirstOrDefaultAsync(
                    p => p.ProductTypeCode == "CHOCOLATE"
                );

            var candyType = await _context.ProductTypes
                .FirstOrDefaultAsync(
                    p => p.ProductTypeCode == "CANDY"
                );

            if (chocolateType == null || candyType == null) {
                return BadRequest(new {
                    message = "제품 유형 정보를 찾을 수 없습니다."
                });
            }

            // 초콜릿 목표 낱개 수
            int targetChocolateCount =
                productionSession.TargetChocolateSetCount
                * chocolateType.UnitPerSet;

            // 목표 달성 여부
            bool isChocolateCompleted =
                productionSession.ChocolateCount
                >= targetChocolateCount;

            bool isCandyCompleted =
                productionSession.CandyCount
                >= productionSession.TargetCandyCount;

            bool isTargetCompleted =
                isChocolateCompleted && isCandyCompleted;

            // 목표 달성 여부에 따라 생산 상태 결정
            if (isTargetCompleted) {
                productionSession.Status = "COMPLETED";
            }
            else {
                productionSession.Status = "CANCELLED";
            }

            productionSession.EndedAt = DateTime.Now;
            productionSession.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            // 현재 세트 수
            int chocolateSetCount =
                chocolateType.UnitPerSet > 0
                    ? productionSession.ChocolateCount
                        / chocolateType.UnitPerSet
                    : 0;

            int candySetCount =
                candyType.UnitPerSet > 0
                    ? productionSession.CandyCount
                        / candyType.UnitPerSet
                    : 0;

            // 진행률
            int chocolateProgress =
                targetChocolateCount > 0
                    ? (int)Math.Round(
                        (double)productionSession.ChocolateCount
                        / targetChocolateCount
                        * 100
                    )
                    : 0;

            int candyProgress =
                productionSession.TargetCandyCount > 0
                    ? (int)Math.Round(
                        (double)productionSession.CandyCount
                        / productionSession.TargetCandyCount
                        * 100
                    )
                    : 0;

            // 생산 종료 MQTT Publish
            await _mqttPublisher.PublishAsync(
                "smart_sorting/production/status",
                new {
                    sessionId = productionSession.SessionId,
                    status = productionSession.Status,

                    chocolate = new {
                        currentCount =
                            productionSession.ChocolateCount,

                        targetCount =
                            targetChocolateCount,

                        unitPerSet =
                            chocolateType.UnitPerSet,

                        setCount =
                            chocolateSetCount,

                        progress =
                            chocolateProgress
                    },

                    candy = new {
                        currentCount =
                            productionSession.CandyCount,

                        targetCount =
                            productionSession.TargetCandyCount,

                        unitPerSet =
                            candyType.UnitPerSet,

                        setCount =
                            candySetCount,

                        progress =
                            candyProgress
                    }
                }
            );

            return Ok(new {
                message = "생산 작업이 종료되었습니다.",
                sessionId = productionSession.SessionId,
                status = productionSession.Status,
                targetChocolateSetCount =
                    productionSession.TargetChocolateSetCount,
                targetCandyCount =
                    productionSession.TargetCandyCount,
                chocolateCount =
                    productionSession.ChocolateCount,
                candyCount =
                    productionSession.CandyCount,
                endedAt =
                    productionSession.EndedAt
            });
        }

    }
}