using EcommerceApi.Dtos;
using EcommerceApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceApi.Controllers;

[ApiController]
[Route("api/categories")]
public class CategoriesController(ICategoryService categories) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CategoryResponse>>> GetAll()
        => Ok(await categories.GetAllAsync());

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CategoryResponse>> GetById(int id)
        => Ok(await categories.GetByIdAsync(id));

    [HttpPost]
    public async Task<ActionResult<CategoryResponse>> Create([FromBody] CategoryRequest request)
    {
        var created = await categories.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<CategoryResponse>> Update(int id, [FromBody] CategoryRequest request)
        => Ok(await categories.UpdateAsync(id, request));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await categories.DeleteAsync(id);
        return NoContent();
    }
}
