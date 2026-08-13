using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSortingServer.DTOs;
using SmartSortingServer.Services;

namespace SmartSortingServer.Controllers {
    [ApiController]
    [Route("api/product-detections")]
    [Authorize]
    public class ProductDetectionsController : ControllerBase {
        private readonly ProductDetectionService _productDetectionService;

        public ProductDetectionsController(
            ProductDetectionService productDetectionService) {

            _productDetectionService = productDetectionService;
        }

        // 제품 감지 결과 저장
        [HttpPost]
        public async Task<IActionResult> CreateProductDetection(
            ProductDetectionRequest request) {

            try {
                var productDetection =
                    await _productDetectionService
                        .CreateProductDetectionAsync(request);

                return Ok(new {
                    message = "제품 감지 결과가 저장되었습니다.",
                    productDetectionId =
                        productDetection.ProductDetectionId,

                    sessionId = productDetection.SessionId,

                    productTypeCode = request.ProductTypeCode,

                    confidence = productDetection.Confidence,

                    classificationStatus =
                        productDetection.ClassificationStatus,

                    detectedAt = productDetection.DetectedAt
                });
            }
            catch (InvalidOperationException ex) {
                return NotFound(new {
                    message = ex.Message
                });
            }
            catch (ArgumentException ex) {
                return BadRequest(new {
                    message = ex.Message
                });
            }
        }
    }
}