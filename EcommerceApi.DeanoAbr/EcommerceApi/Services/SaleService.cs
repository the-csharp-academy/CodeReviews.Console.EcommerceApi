using EcommerceApi.Data;
using EcommerceApi.Dtos;
using EcommerceApi.Models;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApi.Services;

public class SaleService(EcommerceDbContext db) : ISaleService
{
    public async Task<PagedResult<SaleResponse>> GetPagedAsync(SalesQuery query)
    {
        var (page, pageSize) = Paging.Validate(query.Page, query.PageSize);

        if (query.From is DateTime fromDateCheck && query.To is DateTime toDateCheck && fromDateCheck > toDateCheck)
        {
            throw new ValidationException("from must be on or before to.");
        }

        var sales = db.Sales.AsNoTracking().Where(s => !s.IsDeleted);

        if (!string.IsNullOrWhiteSpace(query.Customer))
        {
            var customer = query.Customer.Trim().ToLowerInvariant();
            sales = sales.Where(s => s.CustomerName.ToLower().Contains(customer));
        }
        if (query.From is DateTime fromDate) sales = sales.Where(s => s.SoldAtUtc >= fromDate);
        if (query.To is DateTime toDate) sales = sales.Where(s => s.SoldAtUtc <= toDate);

        sales = ApplySort(sales, query.SortBy, query.SortDir);

        var totalCount = await sales.CountAsync();

        var rows = await sales
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(s => s.SaleItems)
            .ToListAsync();

        var items = rows.Select(ToResponse).ToList();
        return new PagedResult<SaleResponse>(items, page, pageSize, totalCount);
    }

    public async Task<SaleResponse> GetByIdAsync(int id)
    {
        var sale = await db.Sales
            .AsNoTracking()
            .Include(s => s.SaleItems)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

        return sale is null ? throw new NotFoundException($"Sale {id} was not found.") : ToResponse(sale);
    }

    public async Task<SaleResponse> CreateAsync(SaleRequest request)
    {
        if (request.Items is null || request.Items.Count == 0)
        {
            throw new ValidationException("A sale must contain at least one item.");
        }

        var requestedIds = request.Items.Select(i => i.ProductId).ToList();
        if (requestedIds.Count != requestedIds.Distinct().Count())
        {
            throw new ValidationException("Each product may appear only once in a sale.");
        }

        var products = await db.Products
            .Where(p => requestedIds.Contains(p.Id) && !p.IsDeleted)
            .ToListAsync();

        foreach (var id in requestedIds)
        {
            if (products.All(p => p.Id != id)) throw new ValidationException($"Product {id} does not exist.");
        }

        var sale = new Sale
        {
            CustomerName = request.CustomerName.Trim(),
            SoldAtUtc = DateTime.UtcNow
        };

        foreach (var line in request.Items)
        {
            var product = products.Single(p => p.Id == line.ProductId);

            if (line.Quantity > product.StockQuantity)
            {
                throw new ConflictException($"Only {product.StockQuantity} of '{product.Name}' are in stock.");
            }

            product.StockQuantity -= line.Quantity;
            sale.SaleItems.Add(new SaleItem
            {
                ProductId = product.Id,
                ProductName = product.Name,   // receipt snapshot
                Quantity = line.Quantity,
                UnitPrice = product.Price     // receipt snapshot — a later price change cannot rewrite this
            });
        }

        db.Sales.Add(sale);
        await db.SaveChangesAsync();

        return ToResponse(sale);
    }

    public async Task DeleteAsync(int id)
    {
        var sale = await db.Sales
            .Include(s => s.SaleItems)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted)
            ?? throw new NotFoundException($"Sale {id} was not found.");

        var productIds = sale.SaleItems.Select(i => i.ProductId).ToList();

        // Deliberately no !IsDeleted filter: a retired product still gets its stock back.
        var products = await db.Products.Where(p => productIds.Contains(p.Id)).ToListAsync();

        foreach (var item in sale.SaleItems)
        {
            var product = products.FirstOrDefault(p => p.Id == item.ProductId);
            if (product is not null) product.StockQuantity += item.Quantity;
        }

        sale.IsDeleted = true;   // voided, not deleted
        await db.SaveChangesAsync();
    }

    private static SaleResponse ToResponse(Sale sale)
    {
        // Materialised in memory: EF cannot translate the computed Total/LineTotal properties.
        var items = sale.SaleItems
            .OrderBy(i => i.Id)
            .Select(i => new SaleLineResponse(
                i.ProductId, i.ProductName, i.Quantity, i.UnitPrice, i.Quantity * i.UnitPrice))
            .ToList();

        return new SaleResponse(sale.Id, sale.CustomerName, sale.SoldAtUtc, items.Sum(i => i.LineTotal), items);
    }

    private static IQueryable<Sale> ApplySort(IQueryable<Sale> sales, string? sortBy, string? sortDir)
    {
        var key = (sortBy ?? "date").ToLowerInvariant();
        if (key is not ("date" or "total" or "customer"))
        {
            throw new ValidationException("sortBy must be one of: date, total, customer.");
        }

        var direction = (sortDir ?? (key == "date" ? "desc" : "asc")).ToLowerInvariant();
        if (direction is not ("asc" or "desc"))
        {
            throw new ValidationException("sortDir must be 'asc' or 'desc'.");
        }

        var descending = direction == "desc";
        return key switch
        {
            "total" => descending
                ? sales.OrderByDescending(s => s.SaleItems.Sum(i => i.Quantity * i.UnitPrice))
                : sales.OrderBy(s => s.SaleItems.Sum(i => i.Quantity * i.UnitPrice)),
            "customer" => descending
                ? sales.OrderByDescending(s => s.CustomerName)
                : sales.OrderBy(s => s.CustomerName),
            _ => descending ? sales.OrderByDescending(s => s.SoldAtUtc) : sales.OrderBy(s => s.SoldAtUtc)
        };
    }
}
