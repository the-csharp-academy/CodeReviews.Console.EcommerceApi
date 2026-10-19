using Ecommerce.WebAPIs.DTOs;
using Ecommerce.WebAPIs.Services;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.WebAPIs.Controllers
{
    [ApiController]
    [Route("/api/[controller]")]
    public class CategoryController : ControllerBase
    {
        private readonly ICategoryService _categoryService;
        public CategoryController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        [HttpPost]
        public async Task<ActionResult<CategoryResponseDto>> CreateCategory(CategoryRequestDto category)
        {
            var result = await _categoryService.CreateCategory(category);
            return CreatedAtAction(nameof(GetCategoryById), new { id = result.Id }, result);
        }

        [HttpGet]
        public async Task<ActionResult<List<CategoryResponseDto>>> GetAllCategories()
        {
            var result = await _categoryService.GetAllCategories();
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<CategoryResponseDto>> GetCategoryById(int id)
        {
            var result = await _categoryService.GetCategoryById(id);
            if (result == null)
                return NotFound();
            return Ok(result);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<CategoryResponseDto>> UpdateCategory(int id, [FromBody] CategoryRequestDto updatredCategory)
        {
            var result = await _categoryService.UpdateCategory(id, updatredCategory);
            if (result == null)
                return NotFound();
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteCategory(int id)
        {
            var result = await _categoryService.DeleteCategory(id);
            if (result)
                return NoContent();
            return NotFound();
        }
    }
}
