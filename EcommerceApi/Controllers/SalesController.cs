using EcommerceApi.DTO;
using EcommerceApi.DTO.Sale;
using EcommerceApi.Models;
using EcommerceApi.Persistant;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Immutable;
using System.Linq.Dynamic.Core;

namespace EcommerceApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class SalesController : ControllerBase
{
    private readonly ILogger<SalesController> _logger;
    private readonly AppDbContext _dbContext;

    public SalesController(
        ILogger<SalesController> logger,
        AppDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }


    // GET: api/Sales 
    [HttpGet(Name = "GetSales")]
    [ResponseCache(Location = ResponseCacheLocation.Any, Duration = 60)]
    public async Task<ActionResult<RestDTO<SaleDTO[]>>> Get(
        [FromQuery] RequestDTO<SaleDTO> requestDTO)
    {
        var query = _dbContext.Sales
            .AsNoTracking()
            .Where(s => s.DeletedAt == null);

        if (!string.IsNullOrWhiteSpace(requestDTO.FilterQuery))
        {
            requestDTO.FilterQuery = requestDTO.FilterQuery.Trim();

            if (int.TryParse(requestDTO.FilterQuery, out int inputId))
                query = query.Where(s => s.Id == inputId);
        }

        var recordCount = await query.CountAsync();

        var items = await query
            .OrderBy($"{requestDTO.SortColumn} {requestDTO.SortOrder}")
            .Skip(requestDTO.PageIndex * requestDTO.PageSize)
            .Take(requestDTO.PageSize)
            .Select(s => new SaleDTO
            {
                Id = s.Id,
                Quantity = s.Quantity,
                UnitPrice = s.UnitPrice,
                TotalPrice = s.TotalPrice,
                ProductsIds = s.ProductSale.Where(ps => ps.SaleId == s.Id).Select(ps => ps.ProductId).ToList(),
            })
            .ToArrayAsync();

        return Ok(new RestDTO<SaleDTO[]>
        {
            Data = items,
            PageIndex = requestDTO.PageIndex,
            PageSize = requestDTO.PageSize,
            RecordCount = recordCount,
            Links = new List<LinkDTO>
        {
            new(
                href: Url.Action(null, "Sales", requestDTO, Request.Scheme) ?? "",
                rel: "self",
                type: "GET"
            )
        }
        });
    }


    // POST: api/<SalesController>
    [HttpPost(Name = "CreateSale")]
    [ResponseCache(NoStore = true)]
    public async Task<ActionResult<RestDTO<SaleDTO?>>> Post(
        SaleCreateDTO model)
    {
        var newSale = new Sale
        {
            Quantity = model.Quantity,
            UnitPrice = model.UnitPrice,
            TotalPrice = model.TotalPrice,
            SaleDate = DateTime.UtcNow
        };

        if (model.ProductsIds?.Count > 0)
        {
            var existingProductsIds = await _dbContext.Products.Select(p => p.Id).ToHashSetAsync();
            foreach (var productId in model.ProductsIds)
            {
                if (!existingProductsIds.Contains(productId))
                    continue;

                newSale.ProductSale.Add(new ProductSale
                {
                    Sale = newSale,
                    ProductId = productId,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        _dbContext.Sales.Add(newSale);
        await _dbContext.SaveChangesAsync();

        var data = new SaleDTO
        {
            Id = newSale.Id,
            Quantity = newSale.Quantity,
            UnitPrice = newSale.UnitPrice,
            TotalPrice = newSale.TotalPrice,
            ProductsIds = [.. newSale.ProductSale.Select(ps => ps.ProductId)],
        };

        var response = new RestDTO<SaleDTO?>
        {
            Data = data,
            Links =
            [
                new(
                    href: Url.Action(null, "Sales", new { id = newSale.Id }, Request.Scheme) ?? "",
                    rel: "self",
                    type: "GET"
                )
            ]
        };

        return response!;
    }


    // PUT: api/Sales/5
    [HttpPut("{id:int}", Name = "UpdateSale")]
    [ResponseCache(NoStore = true)]
    public async Task<ActionResult<RestDTO<Sale>>> Update(int id, [FromBody] SaleDTO model)
    {
        if (id != model.Id)
            return Problem(
                detail: $"Route ID mismatch Body ID ",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Bad Request",
                type: "https://tools.ietf.org/html/rfc9110#section-15.5.1"
                );

        var sale = await _dbContext.Sales
            .SingleOrDefaultAsync(p => p.Id == id && p.DeletedAt == null);

        if (sale == null)
        {
            return Problem(
                detail: $"Sale with ID {id} was not found.",
                statusCode: StatusCodes.Status404NotFound,
                title: "Not Found",
                type: "https://tools.ietf.org/html/rfc9110#section-15.5.5"
            );
        }

        if (model.Quantity.HasValue)
            sale.Quantity = model.Quantity.Value;

        if (model.UnitPrice.HasValue)
            sale.UnitPrice = model.UnitPrice.Value;

        if (model.TotalPrice.HasValue)
            sale.TotalPrice = model.TotalPrice.Value;


        if (model.ProductsIds != null)
        {
            var existingProductIds = await _dbContext.ProductsSales
                .Where(ps => ps.SaleId == sale.Id)
                .Select(ps => ps.ProductId)
                .ToHashSetAsync();

            var newProductIds = model.ProductsIds.Distinct().Except(existingProductIds);

            var validProductIds = await _dbContext.Products
                .Where(p => newProductIds.Contains(p.Id))
                .Select(p => p.Id)
                .ToListAsync();

            foreach (var productId in validProductIds)
            {
                _dbContext.ProductsSales.Add(new ProductSale
                {
                    SaleId = sale.Id,
                    ProductId = productId,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        sale.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        sale.ProductSale = null!;

        var response = new RestDTO<Sale>
        {
            Data = sale,
            Links = new List<LinkDTO>
        {
            new(
                href: Url.Action(null, "Sales", new { id = sale.Id }, Request.Scheme) ?? "",
                rel: "self",
                type: "GET"
            )
        }
        };

        return response;
    }


    // DELETE: api/Sales/5
    [HttpDelete("{id:int}", Name = "DeleteSale")]
    [ResponseCache(NoStore = true)]
    public async Task<ActionResult<RestDTO<Sale>>> Delete(int id)
    {
        var sale = await _dbContext.Sales
            .SingleOrDefaultAsync(p => p.Id == id && p.DeletedAt == null);

        if (sale == null)
        {
            return Problem(
                detail: $"Sale with ID {id} was not found or has already been deleted.",
                statusCode: StatusCodes.Status404NotFound,
                title: "Not Found",
                type: "https://tools.ietf.org/html/rfc9110#section-15.5.5"
            );
        }

        sale.DeletedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        var response = new RestDTO<Sale>
        {
            Data = sale,
            Links = new List<LinkDTO>
        {
            new(
                href: Url.Action(null, "Sales", new { id = sale.Id }, Request.Scheme) ?? "",
                rel: "self",
                type: "DELETE"
            )
        }
        };

        return Ok(response);
    }

    // GET api/<SalesController>/5
    [HttpGet("{id}", Name = "GetSaleById")]
    public async Task<ActionResult<RestDTO<SaleDTO?>>> GetById(int id)
    {
        var sale = await _dbContext.Sales
            .Where(s => s.Id == id && s.DeletedAt == null)
            .Select(s => new SaleDTO
            {
                Id = s.Id,
                Quantity = s.Quantity,
                UnitPrice = s.UnitPrice,
                TotalPrice = s.TotalPrice,
                ProductsIds = s.ProductSale.Where(ps => ps.SaleId == s.Id).Select(ps => ps.ProductId).ToList(),
            })
            .FirstOrDefaultAsync();

        if (sale == null)
        {
            return Problem(
                detail: $"Sale with ID {id} was not found.",
                statusCode: StatusCodes.Status404NotFound,
                title: "Not Found",
                type: "https://tools.ietf.org/html/rfc9110#section-15.5.5"
            );
        }

        return new RestDTO<SaleDTO?>
        {
            Data = sale,
            Links =
            [
                new(
                    href: Url.Action(null, "Sales", id, Request.Scheme) ?? "",
                    rel: "self",
                    type: "GET"
                )
            ]
        };
    }

}

