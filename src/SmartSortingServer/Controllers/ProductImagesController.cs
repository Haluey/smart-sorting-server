using Microsoft.AspNetCore.Mvc;

namespace SmartSortingServer.Controllers {

    [ApiController]
    [Route("api/product-images")]
    public class ProductImagesController : ControllerBase {

        private readonly IWebHostEnvironment _environment;

        public ProductImagesController(
            IWebHostEnvironment environment) {

            _environment = environment;
        }

        [HttpPost]
        public async Task<IActionResult> UploadImage(
            IFormFile image) {

            if (image == null || image.Length == 0) {
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
                return BadRequest(new {
                    message =
                        "JPG, JPEG, PNG 이미지 파일만 업로드할 수 있습니다."
                });
            }

            const long maxFileSize =
                5 * 1024 * 1024;

            if (image.Length > maxFileSize) {
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

            Directory.CreateDirectory(directoryPath);

            string filePath =
                Path.Combine(
                    directoryPath,
                    fileName
                );

            await using FileStream stream =
                new FileStream(
                    filePath,
                    FileMode.Create
                );

            await image.CopyToAsync(stream);

            string imagePath =
                $"/images/products/{fileName}";

            return Ok(new {
                imagePath
            });
        }
    }
}