using KeremProject1backend.Models.DBs;
using KeremProject1backend.Models.Requests;
using KeremProject1backend.Models.Responses;
using KeremProject1backend.Operations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KeremProject1backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    // TODO: Tüm kontrolöre veya gerekli action'lara Admin yetkilendirmesi ekle
    // [Authorize(Roles = "Admin,AdminAdmin")]
    public class CategoryController : ControllerBase
    {
        private readonly GeneralContext _dbContext;
        private readonly UsersContext _usersContext;
        private readonly ILogger<CategoryController> _logger;

        public CategoryController(GeneralContext dbContext, UsersContext usersContext, ILogger<CategoryController> logger)
        {
            _dbContext = dbContext;
            _usersContext = usersContext;
            _logger = logger;
        }

        // Yeni Kategori Ekleme (Admin)
        [HttpPost("CreateCategory")]
        [Produces("application/json", Type = typeof(BaseResponse))]
        public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryRequest request, [FromHeader] string token)
        {
            var result = await CategoryOperations.CreateCategoryOperation(token, request, _dbContext, _usersContext, _logger);
            return Ok(result);
        }

        // Tüm Kategorileri Listeleme
        [HttpGet("GetAllCategories")]
        [AllowAnonymous] // Şimdilik herkese açık bırakalım
        [Produces("application/json", Type = typeof(BaseResponse))] // List<CategoryResponse> BaseResponse.Response içinde olacak
        public async Task<IActionResult> GetAllCategories()
        {
            var result = await CategoryOperations.GetAllCategoriesOperation(_dbContext, _logger);
            return Ok(result);
        }

        // Kategori Güncelleme (Admin)
        [HttpPut("{categoryId}")]
        [Produces("application/json", Type = typeof(BaseResponse))]
        public async Task<IActionResult> UpdateCategory(int categoryId, [FromBody] UpdateCategoryRequest request, [FromHeader] string token)
        {
            if (categoryId != request.Id)
            {
                return BadRequest(new BaseResponse().GenerateError(4000, "URL ID ile istek ID uyuşmuyor."));
            }
            var result = await CategoryOperations.UpdateCategoryOperation(token, categoryId, request, _dbContext, _usersContext, _logger);
            return Ok(result);
        }

        // Kategori Silme (Admin)
        [HttpDelete("{categoryId}")]
        [Produces("application/json", Type = typeof(BaseResponse))]
        public async Task<IActionResult> DeleteCategory(int categoryId, [FromHeader] string token)
        {
            var result = await CategoryOperations.DeleteCategoryOperation(token, categoryId, _dbContext, _usersContext, _logger);
            return Ok(result);
        }
    }
} 