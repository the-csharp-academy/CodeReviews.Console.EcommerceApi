using EcommerceApi.Data;
using EcommerceApi.Dtos;
using EcommerceApi.Models;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApi.Services;

/// <summary>
/// Deliberately has no Update: the brief forbids changing a product's price, and every other field
/// would be a partial update. Sales keep their own UnitPrice/ProductName snapshots, so history is
/// stable. See README ("Prices are immutable").
/// </summary>
public class ProductService(EcommerceDbContext db) : IProductService
{
    private static readonly string[] AllowedSorts = ["name", "price", "stock", "createdat"];

    public async Task<PagedResult<ProductResponse>> GetPagedAsync(ProductQuery query)
    {
        var (page, pageSize) = Paging.Validate(query.Page, query.PageSize);

        var products = db.Products.AsNoTracking().Where(p => !p.IsDeleted);

        if (query.CategoryId is int categoryId) products = products.Where(p => p.CategoryId == categoryId);
        if (query.MinPrice is decimal minPrice) products = products.Where(p => p.Price >= minPrice);
        if (query.MaxPrice is decimal maxPrice) products = products.Where(p => p.Price <= maxPrice);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLowerInvariant();
            products = products.Where(p => p.Name.ToLower().Contains(search));
        }

        products = ApplySort(products, query.SortBy, query.SortDir);

        var totalCount = await products.CountAsync();

        // Straight projection — EF translates this. Never call a helper method inside Select.
        var rows = await products
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProductResponse(
                p.Id, p.Name, p.Description, p.Price, p.StockQuantity,
                p.CategoryId, p.Category!.Name, p.CreatedAtUtc))
            .ToListAsync();

        return new PagedResult<ProductResponse>(rows, page, pageSize, totalCount);
    }

    public async Task<ProductResponse> GetByIdAsync(int id)
    {
        var product = await db.Products
            .AsNoTracking()
            .Where(p => p.Id == id && !p.IsDeleted)
            .Select(p => new ProductResponse(
                p.Id, p.Name, p.Description, p.Price, p.StockQuantity,
                p.CategoryId, p.Category!.Name, p.CreatedAtUtc))
            .FirstOrDefaultAsync();

        return product ?? throw new NotFoundException($"Product {id} was not found.");
    }

    public async Task<ProductResponse> CreateAsync(ProductRequest request)
    {
        var name = request.Name.Trim();
        if (name.Length == 0) throw new ValidationException("Product name is required.");

        var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == request.CategoryId && !c.IsDeleted)
            ?? throw new ValidationException($"Category {request.CategoryId} does not exist.");

        var lowered = name.ToLowerInvariant();
        if (await db.Products.AnyAsync(p => !p.IsDeleted && p.Name.ToLower() == lowered))
        {
            throw new ConflictException($"A product named '{name}' already exists.");
        }

        var product = new Product
        {
            Name = name,
            Description = (request.Description ?? string.Empty).Trim(),
            Price = request.Price,                  // set once, never updated
            StockQuantity = request.StockQuantity,
            CategoryId = category.Id,
            CreatedAtUtc = DateTime.UtcNow         // set explicitly, not left to a provider default
        };

        db.Products.Add(product);
        await db.SaveChangesAsync();

        return new ProductResponse(product.Id, product.Name, product.Description, product.Price,
            product.StockQuantity, category.Id, category.Name, product.CreatedAtUtc);
    }

    public async Task DeleteAsync(int id)
    {
        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted)
            ?? throw new NotFoundException($"Product {id} was not found.");

        // Sales keep their own name/price snapshots, so retiring a product never rewrites history.
        product.IsDeleted = true;
        await db.SaveChangesAsync();
    }

    private static IQueryable<Product> ApplySort(IQueryable<Product> products, string? sortBy, string? sortDir)
    {
        var key = (sortBy ?? "createdAt").ToLowerInvariant();
        if (!AllowedSorts.Contains(key))
        {
            throw new ValidationException("sortBy must be one of: name, price, stock, createdAt.");
        }

        var direction = (sortDir ?? (key == "createdat" ? "desc" : "asc")).ToLowerInvariant();
        if (direction is not ("asc" or "desc"))
        {
            throw new ValidationException("sortDir must be 'asc' or 'desc'.");
        }

        var descending = direction == "desc";
        return key switch
        {
            "name" => descending ? products.OrderByDescending(p => p.Name) : products.OrderBy(p => p.Name),
            "price" => descending ? products.OrderByDescending(p => p.Price) : products.OrderBy(p => p.Price),
            "stock" => descending ? products.OrderByDescending(p => p.StockQuantity) : products.OrderBy(p => p.StockQuantity),
            _ => descending ? products.OrderByDescending(p => p.CreatedAtUtc) : products.OrderBy(p => p.CreatedAtUtc)
        };
    }
}
