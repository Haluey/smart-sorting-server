using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartSortingServer.Data;
using SmartSortingServer.DTOs;
using SmartSortingServer.Services;

namespace SmartSortingServer.Controllers {
    [ApiController]
    [Route("api/product-detections")]
    [Authorize]
    public class ProductDetectionsController : ControllerBase {
        private readonly ProductDetectionService _productDetectionService;
        private readonly AppDbContext _context;
        private readonly ILogger<ProductDetectionsController> _logger;

        public ProductDetectionsController(
            ProductDetectionService productDetectionService,
            AppDbContext context,
            ILogger<ProductDetectionsController> logger) {

            _productDetectionService = productDetectionService;
            _context = context;
            _logger = logger;
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

                    detectedAt =
                        ToKstDateTimeOffset(
                            productDetection.DetectedAt
                        )
                });
            }
            catch (InvalidOperationException ex) {

                _logger.LogWarning(
                    "[DETECTION] 제품 감지 처리 실패 - Reason: {Message}",
                    ex.Message
                );

                return NotFound(new {
                    message = ex.Message
                });
            }
            catch (ArgumentException ex) {

                _logger.LogWarning(
                    "[DETECTION] 제품 감지 요청 거부 - Reason: {Message}",
                    ex.Message
                );

                return BadRequest(new {
                    message = ex.Message
                });
            }
        }

        // 제품 감지 결과 목록 조회
        [HttpGet]
        public async Task<IActionResult> GetProductDetections(
            int page = 1,
            int pageSize = 15,
            string? productType = null,
            string? status = null,
            string? search = null) {

            if (page < 1) {
                page = 1;
            }

            if (pageSize < 1) {
                pageSize = 15;
            }

            if (pageSize > 100) {
                pageSize = 100;
            }

            var query = _context.ProductDetections
                .AsNoTracking()
                .AsQueryable();

            // 제품 유형 필터
            if (!string.IsNullOrWhiteSpace(productType)) {

                string normalizedProductType =
                    productType.Trim().ToUpper();

                if (normalizedProductType != "CHOCOLATE"
                    && normalizedProductType != "CANDY") {

                    return BadRequest(new {
                        message = "제품 유형은 CHOCOLATE, CANDY만 사용할 수 있습니다."
                    });
                }

                query = query.Where(p =>
                    p.ProductType != null
                    && p.ProductType.ProductTypeCode
                        == normalizedProductType
                );
            }

            // 분류 상태 필터
            if (!string.IsNullOrWhiteSpace(status)) {

                string normalizedStatus =
                    status.Trim().ToUpper();

                if (normalizedStatus != "FAILED") {
                    return BadRequest(new {
                        message = "분류 상태는 FAILED만 사용할 수 있습니다."
                    });
                }

                query = query.Where(p =>
                    p.ClassificationStatus
                        == normalizedStatus
                );
            }

            // 감지 ID 검색
            if (!string.IsNullOrWhiteSpace(search)) {

                string normalizedSearch =
                    search.Trim();

                if (!long.TryParse(
                    normalizedSearch,
                    out long productDetectionId)) {

                    return BadRequest(new {
                        message = "감지 ID는 숫자로 입력해야 합니다."
                    });
                }

                query = query.Where(p =>
                    p.ProductDetectionId
                        == productDetectionId
                );
            }

            // 최신 ID 순
            query = query
                .OrderByDescending(p => p.ProductDetectionId);

            // 전체 개수
            int totalCount =
                await query.CountAsync();

            int totalPages =
                (int)Math.Ceiling(
                    (double)totalCount / pageSize
                );

            var rawItems =
                await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new {
                        productDetectionId =
                            p.ProductDetectionId,

                        sessionId =
                            p.SessionId,

                        productTypeCode =
                            p.ProductTypeId == null
                                ? null
                                : _context.ProductTypes
                                    .Where(t =>
                                        t.ProductTypeId
                                        == p.ProductTypeId
                                    )
                                    .Select(t =>
                                        t.ProductTypeCode
                                    )
                                    .FirstOrDefault(),

                        confidence =
                            p.Confidence,

                        imagePath =
                            p.ImagePath,

                        classificationStatus =
                            p.ClassificationStatus,

                        detectedAt =
                            p.DetectedAt
                    })
                    .ToListAsync();

            var items =
                rawItems.Select(p => new {
                    p.productDetectionId,
                    p.sessionId,
                    p.productTypeCode,
                    p.confidence,
                    p.imagePath,
                    p.classificationStatus,

                    detectedAt =
                        ToKstDateTimeOffset(p.detectedAt)
                });

            return Ok(new {
                items,
                page,
                pageSize,
                totalCount,
                totalPages
            });
        }

        // 제품 감지 결과 상세 조회
        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetProductDetection(long id) {

            var productDetection =
                await _context.ProductDetections
                    .AsNoTracking()
                    .Where(p =>
                        p.ProductDetectionId == id
                    )
                    .Select(p => new {
                        productDetectionId =
                            p.ProductDetectionId,

                        sessionId =
                            p.SessionId,

                        productTypeCode =
                            p.ProductTypeId == null
                                ? null
                                : _context.ProductTypes
                                    .Where(t =>
                                        t.ProductTypeId
                                        == p.ProductTypeId
                                    )
                                    .Select(t =>
                                        t.ProductTypeCode
                                    )
                                    .FirstOrDefault(),

                        confidence =
                            p.Confidence,

                        imagePath =
                            p.ImagePath,

                        classificationStatus =
                            p.ClassificationStatus,

                        detectedAt =
                            p.DetectedAt
                    })
                    .FirstOrDefaultAsync();

            if (productDetection == null) {
                return NotFound(new {
                    message = "제품 감지 결과를 찾을 수 없습니다."
                });
            }

            return Ok(new {
                productDetection.productDetectionId,
                productDetection.sessionId,
                productDetection.productTypeCode,
                productDetection.confidence,
                productDetection.imagePath,
                productDetection.classificationStatus,

                detectedAt =
                    ToKstDateTimeOffset(
                        productDetection.detectedAt
                    )
            });
        }

        // KST(+09:00) 시간으로 변환
        private static DateTimeOffset ToKstDateTimeOffset(
            DateTime dateTime) {

            DateTime unspecifiedDateTime =
                DateTime.SpecifyKind(
                    dateTime,
                    DateTimeKind.Unspecified
                );

            return new DateTimeOffset(
                unspecifiedDateTime,
                TimeSpan.FromHours(9)
            );
        }
    }
}