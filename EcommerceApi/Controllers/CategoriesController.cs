using EcommerceApi.DTO;
using EcommerceApi.DTO.Category;
using EcommerceApi.Models;
using EcommerceApi.Persistant;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;

namespace EcommerceApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CategoriesController : ControllerBase
{
    private readonly ILogger<CategoriesController> _logger;
    private readonly AppDbContext _dbContext;

    public CategoriesController(
        ILogger<CategoriesController> logger,
        AppDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }


    // GET: api/Categories
    [HttpGet(Name = "GetCategories")]
    [ResponseCache(Location = ResponseCacheLocation.Any, Duration = 60)]
    public async Task<ActionResult<RestDTO<Category[]>>> Get(
        [FromQuery] RequestDTO<CategoryDTO> requestDTO)
    {
        var query = _dbContext.Categories
            .AsNoTracking()
            .Where(p => p.DeletedAt == null);

        if (!string.IsNullOrEmpty(requestDTO.FilterQuery?.Trim()))
        {
            query = query.Where(p => p.Name.Contains(requestDTO.FilterQuery));
        }

        var recordCount = await query.CountAsync();


        var items = await query
            .OrderBy($"{requestDTO.SortColumn} {requestDTO.SortOrder}")
            .Skip(requestDTO.PageIndex * requestDTO.PageSize)
            .Take(requestDTO.PageSize)
            .ToArrayAsync();

        return Ok(new RestDTO<Category[]>
        {
            Data = items,
            PageIndex = requestDTO.PageIndex,
            PageSize = requestDTO.PageSize,
            RecordCount = recordCount,
            Links = new List<LinkDTO>
        {
            new(
                href: Url.Action(null, "Categories", requestDTO, Request.Scheme) ?? "",
                rel: "self",
                type: "GET"
            )
        }
        });
    }


    // PUT: api/Categories/5
    [HttpPut("{id:int}", Name = "UpdateCategory")]
    [ResponseCache(NoStore = true)]
    public async Task<ActionResult<RestDTO<Category>>> Update(int id, [FromBody] CategoryDTO model)
    {
        if (id != model.Id)
            return Problem(
                detail: $"Route ID mismatch Body ID ",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Bad Request",
                type: "https://tools.ietf.org/html/rfc9110#section-15.5.1"
                );

        var category = await _dbContext.Categories
            .SingleOrDefaultAsync(p => p.Id == id && p.DeletedAt == null);

        if (category == null)
        {
            return Problem(
                detail: $"Category with ID {id} was not found.",
                statusCode: StatusCodes.Status404NotFound,
                title: "Not Found",
                type: "https://tools.ietf.org/html/rfc9110#section-15.5.5"
            );
        }

       
        if (!string.IsNullOrWhiteSpace(model.Slug) && model.Slug != category.Slug)
        {
            bool skuExists = await _dbContext.Categories
                .AnyAsync(p => p.Slug == model.Slug && p.Id != id);

            if (skuExists)
            {
                var details = new ValidationProblemDetails(new Dictionary<string, string[]>
                {
                    ["Slug"] = new[] { "A Category with this Slug already exists." }
                })
                {
                    Type = "https://tools.ietf.org/html/rfc9110#section-15.5.10",
                    Status = StatusCodes.Status409Conflict,
                    Title = "Conflict"
                };
                details.Extensions["traceId"] = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier;

                return Conflict(details);
            }

            category.Slug = model.Slug;
        }

        if (!string.IsNullOrWhiteSpace(model.Name))
            category.Name = model.Name;

        if (model.ParentId.HasValue && model.ParentId > 0 && model.ParentId != category.Id)
            category.ParentId = model.ParentId.Value;

        category.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        var response = new RestDTO<Category>
        {
            Data = category,
            Links = new List<LinkDTO>
        {
            new(
                href: Url.Action(null, "Categories", new { id = category.Id }, Request.Scheme) ?? "",
                rel: "self",
                type: "GET"
            )
        }
        };

        return response;
    }


    // POST: api/Categories
    [HttpPost(Name = "CreateCategory")]
    [ResponseCache(NoStore = true)]
    public async Task<ActionResult<RestDTO<Category?>>> Post(
        CategoryCreateDTO model)
    {
        bool slugExists = await _dbContext.Categories.AnyAsync(p => p.Slug == model.Slug);
        if (slugExists)
        {
            var details = new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["Slug"] = new[] { "A product with this Slug already exists." }
            })
            {
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.10",
                Status = StatusCodes.Status409Conflict,
                Title = "Conflict"
            };
            details.Extensions["traceId"] = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier;

            return Conflict(details);
        }

        var newCategory = new Category
        {
            Name = model.Name,
            Slug = model.Slug,
            ParentId = model.ParentId,
            CreatedAt = DateTime.UtcNow
        };

       
        _dbContext.Categories.Add(newCategory);
        await _dbContext.SaveChangesAsync();

        var response = new RestDTO<Category>
        {
            Data = newCategory,
            Links =
            [
                new(
                    href: Url.Action(null, "Products", new { id = newCategory.Id }, Request.Scheme) ?? "",
                    rel: "self",
                    type: "GET"
                )
            ]
        };

        return response!;
    }


    // DELETE: api/Categories/5
    [HttpDelete("{id:int}", Name = "DeleteCategory")]
    [ResponseCache(NoStore = true)]
    public async Task<ActionResult<RestDTO<Category>>> Delete(int id)
    {
        var category = await _dbContext.Categories
            .SingleOrDefaultAsync(p => p.Id == id && p.DeletedAt == null);

        if (category == null)
        {
            return Problem(
                detail: $"Category with ID {id} was not found or has already been deleted.",
                statusCode: StatusCodes.Status404NotFound,
                title: "Not Found",
                type: "https://tools.ietf.org/html/rfc9110#section-15.5.5"
            );
        }

        category.DeletedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        var response = new RestDTO<Category>
        {
            Data = category,
            Links = new List<LinkDTO>
        {
            new(
                href: Url.Action(null, "Products", new { id = category.Id }, Request.Scheme) ?? "",
                rel: "self",
                type: "DELETE"
            )
        }
        };

        return Ok(response);
    }

    // GET api/Categories/5
    [HttpGet("{id}", Name = "GetCategoryById")]
    public async Task<ActionResult<RestDTO<Category?>>> GetById(int id)
    {
        var category = await _dbContext.Categories
            .SingleOrDefaultAsync(p => p.Id == id && p.DeletedAt == null);

        if (category == null)
        {
            return Problem(
                detail: $"Category with ID {id} was not found.",
                statusCode: StatusCodes.Status404NotFound,
                title: "Not Found",
                type: "https://tools.ietf.org/html/rfc9110#section-15.5.5"
            );
        }

        return new RestDTO<Category?>
        {
            Data = category,
            Links =
            [
                new(
                    href: Url.Action(null, "Categories", id, Request.Scheme) ?? "",
                    rel: "self",
                    type: "GET"
                )
            ]
        };
    }

}

