using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SmartSortingServer.Controllers {
    [ApiController]
    [Route("api")]
    public class HealthController : ControllerBase {

        // 서버 상태 확인
        [AllowAnonymous]
        [HttpGet("health")]
        public IActionResult Health() {
            return Ok(new {
                status = "OK",
                message = "Smart Sorting Server is running."
            });
        }
    }
}