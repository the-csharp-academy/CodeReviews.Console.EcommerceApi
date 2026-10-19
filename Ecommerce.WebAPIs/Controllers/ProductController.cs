using Ecommerce.WebAPIs.DTOs;
using Ecommerce.WebAPIs.Services;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.WebAPIs.Controllers
{
    [ApiController]
    [Route("/api/[controller]")]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _productService;

        public ProductController(IProductService product)
        {
            _productService = product;
        }

        [HttpPost]
        public async Task<ActionResult<ProductResponseDto>> CreateProduct(ProductRequestDto product)
        {
            var result = await _productService.CreateProduct(product);
            return CreatedAtAction(nameof(GetProductById), new { id = result.Id }, result);
        }

        [HttpGet]
        public async Task<ActionResult<PagedResultDto<ProductResponseDto>>> GetAllProducts([FromQuery] PaginationParams paginationParams)
        {
            var result = await _productService.GetAllProducts(paginationParams);
            if (result == null)
                return NotFound();
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ProductResponseDto>> GetProductById(int id)
        {
            var result = await _productService.GetProductById(id);
            if (result == null)
                return NotFound();
            return Ok(result);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ProductResponseDto?>> UpdateProduct(int id, [FromBody] ProductRequestDto updatedProduct)
        {
            var result = await _productService.UpdateProduct(id, updatedProduct);
            if (result == null)
                return BadRequest();
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<bool>> DeleteProduct(int id)
        {
            var result = await _productService.DeleteProduct(id);
            if (!result)
                return NotFound();
            return NoContent();
        }
    }
}
