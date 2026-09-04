using Microsoft.AspNetCore.Mvc;

namespace SmartSortingServer.Controllers {

    [ApiController]
    [Route("api/product-images")]
    public class ProductImagesController : ControllerBase {

        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ProductImagesController> _logger;

        public ProductImagesController(
            IWebHostEnvironment environment,
            ILogger<ProductImagesController> logger) {

            _environment = environment;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> UploadImage(
            IFormFile? image) {

            if (image == null || image.Length == 0) {

                _logger.LogWarning(
                    "[IMAGE] 이미지 업로드 거부 - Reason: 이미지 파일 없음"
                );

                return BadRequest(new {
                    message = "이미지 파일이 필요합니다."
                });
            }

            string extension =
                Path.GetExtension(image.FileName)
                    .ToLowerInvariant();

            string[] allowedExtensions = {
                ".jpg",
                ".jpeg",
                ".png"
            };

            if (!allowedExtensions.Contains(extension)) {

                _logger.LogWarning(
                    "[IMAGE] 이미지 업로드 거부 - FileName: {FileName}, Reason: 허용되지 않은 확장자",
                    image.FileName
                );

                return BadRequest(new {
                    message =
                        "JPG, JPEG, PNG 이미지 파일만 업로드할 수 있습니다."
                });
            }

            const long maxFileSize =
                5 * 1024 * 1024;

            if (image.Length > maxFileSize) {

                _logger.LogWarning(
                    "[IMAGE] 이미지 업로드 거부 - FileName: {FileName}, Size: {Size}, Reason: 5MB 초과",
                    image.FileName,
                    image.Length
                );

                return BadRequest(new {
                    message =
                        "이미지 크기는 5MB를 초과할 수 없습니다."
                });
            }

            string fileName =
                $"{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}{extension}";

            string directoryPath =
                Path.Combine(
                    _environment.WebRootPath,
                    "images",
                    "products"
                );

            string filePath =
                Path.Combine(
                    directoryPath,
                    fileName
                );

            try {
                Directory.CreateDirectory(directoryPath);

                await using FileStream stream =
                    new FileStream(
                        filePath,
                        FileMode.Create
                    );

                await image.CopyToAsync(stream);
            }
            catch (Exception ex) {

                _logger.LogError(
                    "[IMAGE] 이미지 저장 실패 - FileName: {FileName}, Message: {Message}",
                    fileName,
                    ex.Message
                );

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new {
                        message = "이미지 저장 중 오류가 발생했습니다."
                    }
                );
            }

            string imagePath =
                $"/images/products/{fileName}";

            _logger.LogInformation(
                "[IMAGE] 이미지 업로드 완료 - FileName: {FileName}, Size: {Size}",
                fileName,
                image.Length
            );

            return Ok(new {
                imagePath
            });
        }
    }
}