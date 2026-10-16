using EcommerceApi.Dtos;
using EcommerceApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceApi.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController(IProductService products) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<ProductResponse>>> GetPaged([FromQuery] ProductQuery query)
        => Ok(await products.GetPagedAsync(query));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductResponse>> GetById(int id)
        => Ok(await products.GetByIdAsync(id));

    [HttpPost]
    public async Task<ActionResult<ProductResponse>> Create([FromBody] ProductRequest request)
    {
        var created = await products.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    // No Update action on purpose: prices are immutable (the brief) and an update DTO for every other
    // field would invite the price change back in. PUT returns 405, asserted by a test.
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await products.DeleteAsync(id);
        return NoContent();
    }
}
