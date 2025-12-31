using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.DTOS.Common;
using Shared.Exceptions;
using System.Security.Claims;

namespace Rafedd.Controllers
{
    [ApiController]
    [Route("api/v1")]
    public class FileUploadController : ControllerBase
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<FileUploadController> _logger;
        private readonly long _maxFileSize = 10 * 1024 * 1024; // 10MB
        private readonly string[] _allowedExtensions = { ".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png" };

        public FileUploadController(IWebHostEnvironment environment, ILogger<FileUploadController> logger)
        {
            _environment = environment;
            _logger = logger;
        }

        private string? GetUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        [HttpPost("upload")]
        [Authorize(Roles = "Employee,Manager")]
        [RequestSizeLimit(10485760)] // 10MB
        [ProducesResponseType(typeof(ApiResponse<FileUploadResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse<FileUploadResponse>), 400)]
        public async Task<ActionResult<ApiResponse<FileUploadResponse>>> UploadFile(IFormFile file)
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    throw new UnauthorizedException("User ID not found");
                }

                // Validate file exists
                if (file == null || file.Length == 0)
                {
                    throw new BadRequestException("لم يتم اختيار ملف");
                }

                // Validate file size
                if (file.Length > _maxFileSize)
                {
                    throw new BadRequestException($"حجم الملف يتجاوز الحد الأقصى المسموح به (10 ميجابايت)");
                }

                // Validate file extension
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!_allowedExtensions.Contains(extension))
                {
                    throw new BadRequestException($"نوع الملف غير مسموح به. الأنواع المسموحة: {string.Join(", ", _allowedExtensions)}");
                }

                // Generate unique filename
                var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
                var uniqueFileName = $"{userId}_{timestamp}_{Guid.NewGuid()}{extension}";

                // Create uploads directory if it doesn't exist
                var uploadsPath = Path.Combine(_environment.ContentRootPath, "uploads");
                if (!Directory.Exists(uploadsPath))
                {
                    Directory.CreateDirectory(uploadsPath);
                }

                // Save file
                var filePath = Path.Combine(uploadsPath, uniqueFileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                _logger.LogInformation("File uploaded: {FileName} by User: {UserId}, Size: {Size} bytes",
                    uniqueFileName, userId, file.Length);

                // Return file info
                var response = new FileUploadResponse
                {
                    Url = $"/uploads/{uniqueFileName}",
                    Filename = file.FileName,
                    Size = file.Length
                };

                return Ok(ApiResponse<FileUploadResponse>.SuccessResponse(response, "تم رفع الملف بنجاح"));
            }
            catch (BadRequestException)
            {
                throw;
            }
            catch (UnauthorizedException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file");
                throw new BadRequestException("حدث خطأ أثناء رفع الملف");
            }
        }

        [HttpGet("uploads/{filename}")]
        [Authorize]
        public IActionResult DownloadFile(string filename)
        {
            try
            {
                // Security: Validate filename to prevent directory traversal
                if (string.IsNullOrEmpty(filename) || filename.Contains("..") || filename.Contains("/") || filename.Contains("\\"))
                {
                    throw new BadRequestException("اسم الملف غير صالح");
                }

                var uploadsPath = Path.Combine(_environment.ContentRootPath, "uploads");
                var filePath = Path.Combine(uploadsPath, filename);

                if (!System.IO.File.Exists(filePath))
                {
                    throw new NotFoundException("الملف غير موجود");
                }

                var memory = new MemoryStream();
                using (var stream = new FileStream(filePath, FileMode.Open))
                {
                    stream.CopyTo(memory);
                }
                memory.Position = 0;

                var contentType = GetContentType(filePath);
                return File(memory, contentType, filename);
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading file {Filename}", filename);
                throw new BadRequestException("حدث خطأ أثناء تحميل الملف");
            }
        }

        [HttpDelete("uploads/{filename}")]
        [Authorize(Roles = "Employee,Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        public IActionResult DeleteFile(string filename)
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    throw new UnauthorizedException("User ID not found");
                }

                // Security: Validate filename
                if (string.IsNullOrEmpty(filename) || filename.Contains("..") || filename.Contains("/") || filename.Contains("\\"))
                {
                    throw new BadRequestException("اسم الملف غير صالح");
                }

                // Security: Check if user owns the file (filename starts with userId)
                var isAdmin = User.IsInRole("Admin");
                if (!isAdmin && !filename.StartsWith(userId))
                {
                    throw new UnauthorizedException("غير مصرح لك بحذف هذا الملف");
                }

                var uploadsPath = Path.Combine(_environment.ContentRootPath, "uploads");
                var filePath = Path.Combine(uploadsPath, filename);

                if (!System.IO.File.Exists(filePath))
                {
                    throw new NotFoundException("الملف غير موجود");
                }

                System.IO.File.Delete(filePath);

                _logger.LogInformation("File deleted: {Filename} by User: {UserId}", filename, userId);

                return Ok(ApiResponse.SuccessResponse("تم حذف الملف بنجاح"));
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (UnauthorizedException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file {Filename}", filename);
                throw new BadRequestException("حدث خطأ أثناء حذف الملف");
            }
        }

        private string GetContentType(string path)
        {
            var extension = Path.GetExtension(path).ToLowerInvariant();
            return extension switch
            {
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                _ => "application/octet-stream"
            };
        }
    }

    public class FileUploadResponse
    {
        public string Url { get; set; } = null!;
        public string Filename { get; set; } = null!;
        public long Size { get; set; }
    }
}
