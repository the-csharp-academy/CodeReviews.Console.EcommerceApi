using EcommerceApi.DTO;
using EcommerceApi.DTO.Product;
using EcommerceApi.Models;
using EcommerceApi.Persistant;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Immutable;
using System.Linq.Dynamic.Core;

namespace EcommerceApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ProductsController : ControllerBase
{
    private readonly ILogger<ProductsController> _logger;
    private readonly AppDbContext _dbContext;

    public ProductsController(
        ILogger<ProductsController> logger,
        AppDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }


    // GET: api/Products 
    [HttpGet(Name = "GetProducts")]
    [ResponseCache(Location = ResponseCacheLocation.Any, Duration = 60)]
    public async Task<ActionResult<RestDTO<Product[]>>> Get(
        [FromQuery] RequestDTO<SaleDTO> requestDTO)
    {
        var query = _dbContext.Products
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

        return Ok(new RestDTO<Product[]>
        {
            Data = items,
            PageIndex = requestDTO.PageIndex,
            PageSize = requestDTO.PageSize,
            RecordCount = recordCount,
            Links = new List<LinkDTO>
        {
            new(
                href: Url.Action(null, "Products", requestDTO, Request.Scheme) ?? "",
                rel: "self",
                type: "GET"
            )
        }
        });
    }


    // POST: api/<ProductsController>
    [HttpPost(Name = "CreateProduct")]
    [ResponseCache(NoStore = true)]
    public async Task<ActionResult<RestDTO<Product?>>> Post(
        SaleCreateDTO model)
    {
        bool skuExists = await _dbContext.Products.AnyAsync(p => p.Sku == model.Sku);
        if (skuExists)
        {
            var details = new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["Sku"] = new[] { "A product with this SKU already exists." }
            })
            {
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.10",
                Status = StatusCodes.Status409Conflict,
                Title = "Conflict"
            };
            details.Extensions["traceId"] = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier;

            return Conflict(details);
        }

        if (model.CategoryId != null)
        {
            bool categoryExists = await _dbContext.Categories.AnyAsync(c => c.Id == model.CategoryId);
            if (!categoryExists)
                return NotFound(new { message = "Category not found." });
        }


        var newProduct = new Product
        {
            Name = model.Name,
            Sku = model.Sku,
            Price = model.Price,
            StockQuantity = model.StockQuantity,
            IsActive = model.IsActive,
            CategoryId = model.CategoryId,
            CreatedAt = DateTime.UtcNow
        };

        if (model.SalesIds?.Count > 0)
        {
            var existingSalesIds = await _dbContext.Sales.Select(s => s.Id).ToHashSetAsync();
            foreach (var saleId in model.SalesIds)
            {
                if (!existingSalesIds.Contains(saleId))
                    continue;

                newProduct.ProductSale.Add(new ProductSale
                {
                    Product = newProduct,
                    SaleId = saleId,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        _dbContext.Products.Add(newProduct);
        await _dbContext.SaveChangesAsync();

        var response = new RestDTO<Product>
        {
            Data = newProduct,
            Links =
            [
                new(
                    href: Url.Action(null, "Products", new { id = newProduct.Id }, Request.Scheme) ?? "",
                    rel: "self",
                    type: "GET"
                )
            ]
        };

        return response!;
    }


    // PUT: api/Products/5
    [HttpPut("{id:int}", Name = "UpdateProduct")]
    [ResponseCache(NoStore = true)]
    public async Task<ActionResult<RestDTO<Product>>> Update(int id, [FromBody] SaleDTO model)
    {
        if (id != model.Id)
            return Problem(
                detail: $"Route ID mismatch Body ID ",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Bad Request",
                type: "https://tools.ietf.org/html/rfc9110#section-15.5.1"
                );

        var product = await _dbContext.Products
            .SingleOrDefaultAsync(p => p.Id == id && p.DeletedAt == null);

        if (product == null)
        {
            return Problem(
                detail: $"Product with ID {id} was not found.",
                statusCode: StatusCodes.Status404NotFound,
                title: "Not Found",
                type: "https://tools.ietf.org/html/rfc9110#section-15.5.5"
            );
        }

        if (model.CategoryId.HasValue)
        {
            bool categoryExists = await _dbContext.Categories
                .AnyAsync(c => c.Id == model.CategoryId.Value);

            if (!categoryExists)
            {
                return Problem(
                    detail: $"Category with ID {model.CategoryId.Value} was not found.",
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Not Found",
                    type: "https://tools.ietf.org/html/rfc9110#section-15.5.5"
                );
            }

            product.CategoryId = model.CategoryId.Value;
        }

        if (!string.IsNullOrWhiteSpace(model.Sku) && model.Sku != product.Sku)
        {
            bool skuExists = await _dbContext.Products
                .AnyAsync(p => p.Sku == model.Sku && p.Id != id);

            if (skuExists)
            {
                var details = new ValidationProblemDetails(new Dictionary<string, string[]>
                {
                    ["Sku"] = new[] { "A product with this SKU already exists." }
                })
                {
                    Type = "https://tools.ietf.org/html/rfc9110#section-15.5.10",
                    Status = StatusCodes.Status409Conflict,
                    Title = "Conflict"
                };
                details.Extensions["traceId"] = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier;

                return Conflict(details);
            }

            product.Sku = model.Sku;
        }

        if (!string.IsNullOrWhiteSpace(model.Name))
            product.Name = model.Name;

        if (model.Price.HasValue)
            product.Price = model.Price.Value;

        if (model.StockQuantity.HasValue)
            product.StockQuantity = model.StockQuantity.Value;

        if (model.IsActive.HasValue)
            product.IsActive = model.IsActive.Value;


        if (model.SalesIds != null)
        {
            var existingSaleIds = await _dbContext.ProductsSales
                .Where(ps => ps.ProductId == product.Id)
                .Select(ps => ps.SaleId)
                .ToHashSetAsync();

            var newSaleIds = model.SalesIds.Distinct().Except(existingSaleIds);

            var validSaleIds = await _dbContext.Sales
                .Where(s => newSaleIds.Contains(s.Id))
                .Select(s => s.Id)
                .ToListAsync();

            foreach (var saleId in validSaleIds)
            {
                _dbContext.ProductsSales.Add(new ProductSale
                {
                    ProductId = product.Id,
                    SaleId = saleId,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        product.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        product.ProductSale = null!;

        var response = new RestDTO<Product>
        {
            Data = product,
            Links = new List<LinkDTO>
        {
            new(
                href: Url.Action(null, "Products", new { id = product.Id }, Request.Scheme) ?? "",
                rel: "self",
                type: "GET"
            )
        }
        };

        return response;
    }


    // DELETE: api/Products/5
    [HttpDelete("{id:int}", Name = "DeleteProduct")]
    [ResponseCache(NoStore = true)]
    public async Task<ActionResult<RestDTO<Product>>> Delete(int id)
    {
        var product = await _dbContext.Products
            .SingleOrDefaultAsync(p => p.Id == id && p.DeletedAt == null);

        if (product == null)
        {
            return Problem(
                detail: $"Product with ID {id} was not found or has already been deleted.",
                statusCode: StatusCodes.Status404NotFound,
                title: "Not Found",
                type: "https://tools.ietf.org/html/rfc9110#section-15.5.5"
            );
        }

        product.DeletedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        var response = new RestDTO<Product>
        {
            Data = product,
            Links = new List<LinkDTO>
        {
            new(
                href: Url.Action(null, "Products", new { id = product.Id }, Request.Scheme) ?? "",
                rel: "self",
                type: "DELETE"
            )
        }
        };

        return Ok(response);
    }

    // GET api/<ProductsController>/5
    [HttpGet("{id}", Name = "GetProductById")]
    public async Task<ActionResult<RestDTO<Product?>>> GetById(int id)
    {
        var product = await _dbContext.Products
            .SingleOrDefaultAsync(p => p.Id == id && p.DeletedAt == null);

        if (product == null)
        {
            return Problem(
                detail: $"Product with ID {id} was not found.",
                statusCode: StatusCodes.Status404NotFound,
                title: "Not Found",
                type: "https://tools.ietf.org/html/rfc9110#section-15.5.5"
            );
        }

        return new RestDTO<Product?>
        {
            Data = product,
            Links =
            [
                new(
                    href: Url.Action(null, "Products", id, Request.Scheme) ?? "",
                    rel: "self",
                    type: "GET"
                )
            ]
        };
    }
}

