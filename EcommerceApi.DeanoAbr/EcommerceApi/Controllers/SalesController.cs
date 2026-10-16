using EcommerceApi.Dtos;
using EcommerceApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceApi.Controllers;

[ApiController]
[Route("api/sales")]
public class SalesController(ISaleService sales) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<SaleResponse>>> GetPaged([FromQuery] SalesQuery query)
        => Ok(await sales.GetPagedAsync(query));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SaleResponse>> GetById(int id)
        => Ok(await sales.GetByIdAsync(id));

    [HttpPost]
    public async Task<ActionResult<SaleResponse>> Create([FromBody] SaleRequest request)
    {
        var created = await sales.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>
    /// Void a sale (soft delete) and return the reserved stock to the catalogue.
    /// Sales are never updated: a wrong sale is voided and re-entered, so the receipt trail stays intact.
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await sales.DeleteAsync(id);
        return NoContent();
    }
}
