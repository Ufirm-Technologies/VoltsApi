using Microsoft.AspNetCore.Mvc;

namespace vaulterpAPI.Controllers.Common
{
    [ApiController]
    [Route("api/[controller]")]
    public class ImageUploadController : ControllerBase
    {
        private readonly IWebHostEnvironment _environment;

        public ImageUploadController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadImage([FromForm] UploadRequest request)
        {
            var file = request.File;

            if (file == null || file.Length == 0)
                return BadRequest(new { message = "No file provided." });

            var uploadPath = Path.Combine(_environment.WebRootPath, "uploads");
            if (!Directory.Exists(uploadPath))
                Directory.CreateDirectory(uploadPath);

            var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(uploadPath, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var imageUrl = $"{baseUrl}/uploads/{uniqueFileName}";

            return Ok(new { url = imageUrl });
        }

    }

    public class UploadRequest
    {
        [FromForm(Name = "file")] // 👈 ensures Swagger binds correctly
        public IFormFile File { get; set; }
    }
}
