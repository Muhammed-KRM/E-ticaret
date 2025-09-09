using KeremProject1backend.Models.DBs;
using KeremProject1backend.Models.Requests;
using KeremProject1backend.Models.Responses;
using KeremProject1backend.Operations;
using Microsoft.AspNetCore.Authorization; // AllowAnonymous için
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using KeremProject1backend.Services;
using Microsoft.AspNetCore.Http;
using System; // Exception için eklendi
using Microsoft.AspNetCore.WebUtilities;

namespace KeremProject1backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductController : ControllerBase
    {
        private readonly GeneralContext _dbContext;
        private readonly UsersContext _usersContext; // Added
        private readonly ILogger<ProductController> _logger; // Added
        private readonly FileService _fileService; // Dosya işlemleri için eklendi

        public ProductController(
            GeneralContext dbContext, 
            UsersContext usersContext, 
            ILogger<ProductController> logger,
            FileService fileService) // FileService eklendi
        {
            _dbContext = dbContext;
            _usersContext = usersContext;
            _logger = logger;
            _fileService = fileService;
        }

        // Güvenli bir baseUrl oluştur
        private string GetBaseUrl()
        {
            return $"{Request.Scheme}://{Request.Host}";
        }

        // Yeni Ürün Ekleme (Resim ile Birlikte)
        [HttpPost("CreateProduct")]
        [Produces("application/json", Type = typeof(BaseResponse))]
        [Consumes("multipart/form-data")] // Swagger ve model binding için önemli
        public async Task<IActionResult> CreateProduct([FromForm] CreateProductWithImageRequest request, [FromHeader] string token)
        {
            try
            {
                var baseUrl = GetBaseUrl();
                var result = await ProductOperations.CreateProductWithImageOperation(
                    token,
                    request,
                    _dbContext,
                    _usersContext,
                    _fileService,
                    _logger,
                    baseUrl);

                if (result.Errored)
                {
                    // Daha spesifik hata yönetimi için BaseResponse modelinizin yapısına göre
                    // (örn: ErrorCode, ErrorMessage gibi alanlar) güncelleme yapabilirsiniz.
                    return BadRequest(result);
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CreateProduct controller action sırasında genel bir hata oluştu.");
                return StatusCode(500, new BaseResponse().GenerateError(5000, "Sunucuda beklenmeyen bir hata oluştu."));
            }
        }

        //// Ürün Resmini Yükleme (Admin Gerekli) - Resim yükleme için ayrı endpoint
        //[HttpPost("{productId}/UploadImage")]
        //[Produces("application/json", Type = typeof(BaseResponse))]
        //public async Task<IActionResult> UploadProductImage(int productId, [FromForm] IFormFile productImage, [FromHeader] string token)
        //{
        //    // Product ID kontrolü
        //    var product = await _dbContext.Products.FindAsync(productId);
        //    if (product == null)
        //    {
        //        return BadRequest(new BaseResponse().GenerateError(4404, "Ürün bulunamadı"));
        //    }
            
        //    // Admin kontrolü ve resim yükleme
        //    if (productImage != null && productImage.Length > 0)
        //    {
        //        try
        //        {
        //            var result = await ProductOperations.UploadProductImageOperation(
        //                token,
        //                productId,
        //                productImage,
        //                _dbContext,
        //                _usersContext,
        //                _fileService,
        //                _logger);
                        
        //            return Ok(result);
        //        }
        //        catch (Exception ex)
        //        {
        //            _logger.LogError(ex, $"Resim yükleme hatası: ProductId={productId}");
        //            return BadRequest(new BaseResponse().GenerateError(5000, "Resim yüklenirken hata oluştu"));
        //        }
        //    }
            
        //    return BadRequest(new BaseResponse().GenerateError(4000, "Geçerli bir resim dosyası gönderilmedi"));
        //}

        // Ürün Detayı Getirme (Herkese Açık)
        [HttpGet("{productId}")]
        [AllowAnonymous] // Herkes erişebilir
        [Produces("application/json", Type = typeof(BaseResponse))] // BaseResponse döndürecek
        public async Task<IActionResult> GetProductById(int productId)
        {
            var baseUrl = GetBaseUrl();
            var result = await ProductOperations.GetProductByIdOperation(productId, _dbContext, _logger, baseUrl);
            return Ok(result);
        }

        // Tüm Ürünleri Listeleme (Filtreleme/Sayfalama ile güncellendi)
        [HttpGet("GetAllProducts")]
        [AllowAnonymous] // Herkes erişebilir (Admin için token kontrolü operasyonda)
        [Produces("application/json", Type = typeof(BaseResponse))] 
        public async Task<IActionResult> GetAllProducts([FromQuery] GetAllProductsRequest filters, [FromHeader] string? token = null)
        {
            var baseUrl = GetBaseUrl();
            var result = await ProductOperations.GetAllProductsOperation(token, filters, _dbContext, _usersContext, _logger, baseUrl);
            return Ok(result);
        }

        // Ürün Güncelleme (Admin Gerekli)
        [HttpPut("{productId}")]
        [Produces("application/json", Type = typeof(BaseResponse))]
        public async Task<IActionResult> UpdateProduct(int productId, [FromBody] UpdateProductRequest request, [FromHeader] string token)
        {
            if (productId != request.Id)
            {
                 return BadRequest(new BaseResponse().GenerateError(4000, "URL ID ile istek ID uyuşmuyor."));
            }
            
            var baseUrl = GetBaseUrl();
            var result = await ProductOperations.UpdateProductOperation(
                token,
                productId,
                request,
                _dbContext,
                _usersContext,
                _logger,
                baseUrl);
                
            return Ok(result);
        }

        // Ürün Silme/Pasifleştirme (Admin Gerekli)
        [HttpDelete("{productId}")]
        [Produces("application/json", Type = typeof(BaseResponse))]
        public async Task<IActionResult> DeleteProduct(int productId, [FromHeader] string token)
        {
            var result = await ProductOperations.DeleteProductOperation(token, productId, _dbContext, _usersContext, _logger);
            return Ok(result);
        }

        // Ürün Arama (Herkese Açık)
        [HttpPost("Search")]
        [AllowAnonymous] // Herkes erişebilir
        [Produces("application/json", Type = typeof(BaseResponse))]
        public async Task<IActionResult> SearchProducts([FromBody] SearchProductsRequest request)
        {
            try
            {
                // Null kontrolleri
                request ??= new SearchProductsRequest();
                
                // Gelen isteği loglayalım
                _logger.LogInformation($"Arama isteği alındı: SearchTerm='{request.SearchTerm}', ExactMatch={request.ExactMatch}, SortOption={request.SortOption}");
                
                var baseUrl = GetBaseUrl();
                var result = await ProductOperations.SearchProductsOperation(
                    request,
                    _dbContext,
                    _logger,
                    baseUrl);
                
                // Sonucu loglayalım (kaç ürün bulunduğunu)
                var data = result.Response as dynamic;
                if (data != null)
                {
                    _logger.LogInformation($"Arama sonucu: {data.TotalCount} ürün bulundu");
                }
                    
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SearchProducts controller action sırasında genel bir hata oluştu.");
                return StatusCode(500, new BaseResponse().GenerateError(5000, "Sunucuda beklenmeyen bir hata oluştu."));
            }
        }
    }
} 