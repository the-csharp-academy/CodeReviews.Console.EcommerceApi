using Ecommerce.WebAPIs.DTOs;
using Ecommerce.WebAPIs.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.WebAPIs.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly AppDbContext _dbContext;

        public CategoryService(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<CategoryResponseDto> CreateCategory(CategoryRequestDto category)
        {
            var entity = new CategoryEntity
            {
                Name = category.Name
            };
            _dbContext.Categories.Add(entity);
            await _dbContext.SaveChangesAsync();
            return MapToDto(entity);
        }


        public async Task<List<CategoryResponseDto>> GetAllCategories()
        {
            var entities = await _dbContext.Categories
                .AsNoTracking()
                .ToListAsync();
            return entities.Select(MapToDto).ToList();
        }

        public async Task<CategoryResponseDto?> GetCategoryById(int id)
        {
            var entity = await _dbContext.Categories
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);

            if (entity != null)
                return MapToDto(entity);
            return null;
        }

        public async Task<CategoryResponseDto> UpdateCategory(int id, CategoryRequestDto updatredCategory)
        {
            var category = await _dbContext.Categories.FirstOrDefaultAsync(c => c.Id == id);
            if (category == null)
                throw new ArgumentException($"No such cattegory with id {id}");
            category.Name = updatredCategory.Name;
            await _dbContext.SaveChangesAsync();
            return MapToDto(category);
        }

        public async Task<bool> DeleteCategory(int id)
        {
            var category = await _dbContext.Categories.FirstOrDefaultAsync(x => x.Id == id);
            if (category == null)
                return false;
            _dbContext.Remove(category);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        private static CategoryResponseDto MapToDto(CategoryEntity category)
        {
            return new CategoryResponseDto
            {
                Id = category.Id,
                Name = category.Name,
            };
        }
    }
}
