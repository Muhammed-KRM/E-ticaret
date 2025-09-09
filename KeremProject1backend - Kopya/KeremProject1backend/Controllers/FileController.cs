using KeremProject1backend.Models.DBs;
using KeremProject1backend.Models.Requests;
using KeremProject1backend.Models.Responses;
using KeremProject1backend.Operations;
using KeremProject1backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace KeremProject1backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FileController : ControllerBase
    {
        private readonly ILogger<FileController> _logger;
        private readonly FileService _fileService;
        private readonly UsersContext _usersContext;
        private readonly GeneralContext _dbContext;

        public FileController(
            ILogger<FileController> logger,
            FileService fileService,
            UsersContext usersContext,
            GeneralContext dbContext)
        {
            _logger = logger;
            _fileService = fileService;
            _usersContext = usersContext;
            _dbContext = dbContext;
        }

        // Dosya yükleme - hem form verisi hem JSON olarak kabul edebilir
        [HttpPost("Upload")]
        [Produces("application/json", Type = typeof(BaseResponse))]
        public async Task<IActionResult> UploadFile([FromForm] FileUploadRequest request, [FromHeader] string? token = null)
        {
            var result = await FileOperations.UploadFileOperation(request, token ?? string.Empty, _fileService, _usersContext, _logger);
            return Ok(result);
        }

        // Çoklu dosya yükleme
        [HttpPost("UploadMultiple")]
        [Produces("application/json", Type = typeof(BaseResponse))]
        public async Task<IActionResult> UploadMultipleFiles([FromForm] MultipleFileUploadRequest request, [FromHeader] string? token = null)
        {
            var result = await FileOperations.UploadMultipleFilesOperation(request, token ?? string.Empty, _fileService, _usersContext, _logger);
            return Ok(result);
        }

        // Dosya indirme
        [HttpGet("Download/{*fileName}")]
        public async Task<IActionResult> DownloadFile(string fileName, [FromHeader] string? token = null)
        {
            var result = await FileOperations.DownloadFileOperation(fileName, token ?? string.Empty, _fileService, _usersContext, _dbContext, _logger);
            
            if (result.Errored)
            {
                if (result.Response as FileDownloadResponse == null)
                {
                     return NotFound(result);
                }
                return BadRequest(result); 
            }

            var fileResult = result.Response as FileDownloadResponse;
            if (fileResult == null) 
            {
                _logger.LogWarning($"DownloadFile: FileDownloadResponse null döndü (Errored false olmasına rağmen), dosya adı: {fileName}");
                return NotFound(new BaseResponse().GenerateError(4404, "İndirilecek dosya bilgisi bulunamadı."));
            }

            return File(fileResult.FileData, fileResult.ContentType, fileResult.FileName);

        }

        // Dosya silme
        [HttpDelete("{fileId}")]
        [Produces("application/json", Type = typeof(BaseResponse))]
        public async Task<IActionResult> DeleteFile(int fileId, [FromHeader] string token)
        {
            var result = await FileOperations.DeleteFileOperation(fileId, token, _fileService, _usersContext, _dbContext, _logger);
            return Ok(result);
        }

        // Dosya bilgilerini getirme
        [HttpGet("Info/{fileId}")]
        [Produces("application/json", Type = typeof(BaseResponse))]
        [AllowAnonymous]
        public async Task<IActionResult> GetFileInfo(int fileId)
        {
            var result = await FileOperations.GetFileInfoOperation(fileId, _dbContext, _logger);
            return Ok(result);
        }
    }
} 