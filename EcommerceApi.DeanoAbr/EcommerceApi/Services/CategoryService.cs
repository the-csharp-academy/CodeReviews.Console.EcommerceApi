using EcommerceApi.Data;
using EcommerceApi.Dtos;
using EcommerceApi.Models;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApi.Services;

public class CategoryService(EcommerceDbContext db) : ICategoryService
{
    public async Task<IReadOnlyList<CategoryResponse>> GetAllAsync()
    {
        var rows = await db.Categories
            .AsNoTracking()
            .Where(c => !c.IsDeleted)
            .OrderBy(c => c.Name)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.Description,
                ProductCount = c.Products.Count(p => !p.IsDeleted)
            })
            .ToListAsync();

        return rows.Select(r => new CategoryResponse(r.Id, r.Name, r.Description, r.ProductCount)).ToList();
    }

    public async Task<CategoryResponse> GetByIdAsync(int id)
    {
        var row = await db.Categories
            .AsNoTracking()
            .Where(c => c.Id == id && !c.IsDeleted)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.Description,
                ProductCount = c.Products.Count(p => !p.IsDeleted)
            })
            .FirstOrDefaultAsync();

        return row is null
            ? throw new NotFoundException($"Category {id} was not found.")
            : new CategoryResponse(row.Id, row.Name, row.Description, row.ProductCount);
    }

    public async Task<CategoryResponse> CreateAsync(CategoryRequest request)
    {
        var name = request.Name.Trim();
        await EnsureNameIsFreeAsync(name, null);

        var category = new Category
        {
            Name = name,
            Description = (request.Description ?? string.Empty).Trim()
        };

        db.Categories.Add(category);
        await db.SaveChangesAsync();

        return new CategoryResponse(category.Id, category.Name, category.Description, 0);
    }

    public async Task<CategoryResponse> UpdateAsync(int id, CategoryRequest request)
    {
        var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted)
            ?? throw new NotFoundException($"Category {id} was not found.");

        var name = request.Name.Trim();
        await EnsureNameIsFreeAsync(name, id);

        category.Name = name;
        category.Description = (request.Description ?? string.Empty).Trim();
        await db.SaveChangesAsync();

        var productCount = await db.Products.CountAsync(p => p.CategoryId == id && !p.IsDeleted);
        return new CategoryResponse(category.Id, category.Name, category.Description, productCount);
    }

    public async Task DeleteAsync(int id)
    {
        var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted)
            ?? throw new NotFoundException($"Category {id} was not found.");

        var activeProducts = await db.Products.CountAsync(p => p.CategoryId == id && !p.IsDeleted);
        if (activeProducts > 0)
        {
            throw new ConflictException($"Category {id} still has {activeProducts} active products.");
        }

        category.IsDeleted = true;   // retail practice: records are retired, never removed
        await db.SaveChangesAsync();
    }

    private async Task EnsureNameIsFreeAsync(string name, int? excludeId)
    {
        if (name.Length == 0) throw new ValidationException("Category name is required.");

        var lowered = name.ToLowerInvariant();
        var exists = await db.Categories.AnyAsync(c =>
            !c.IsDeleted && c.Id != (excludeId ?? 0) && c.Name.ToLower() == lowered);

        if (exists) throw new ConflictException($"A category named '{name}' already exists.");
    }
}
