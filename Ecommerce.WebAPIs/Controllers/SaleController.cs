using Ecommerce.WebAPIs.DTOs;
using Ecommerce.WebAPIs.Services;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.WebAPIs.Controllers
{
    [ApiController]
    [Route("/api/[controller]")]
    public class SaleController : ControllerBase
    {
        private readonly ISaleService _saleService;

        public SaleController(ISaleService saleService)
        {
            _saleService = saleService;
        }

        [HttpPost]
        public async Task<ActionResult<SaleResponseDto>> CreateSale(SaleRequestDto sale)
        {
            var result = await _saleService.CreateSale(sale);
            return CreatedAtAction(nameof(GetSaleById), new { id = result.Id }, result);
        }

        [HttpGet]
        public async Task<ActionResult<PagedResultDto<SaleResponseDto>>> GetAllSales([FromQuery] PaginationParams paginationParams)
        {
            var result = await _saleService.GetAllSales(paginationParams);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<SaleResponseDto>> GetSaleById(int id)
        {
            var result = await _saleService.GetSalesById(id);
            if (result == null)
                return NotFound();
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<bool>> DeleteSale(int id)
        {
            var result = await _saleService.DeleteSale(id);
            if (!result)
                return NotFound();
            return NoContent();
        }
    }
}
