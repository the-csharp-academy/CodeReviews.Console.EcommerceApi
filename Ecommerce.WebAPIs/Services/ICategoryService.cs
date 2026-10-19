using Ecommerce.WebAPIs.DTOs;

namespace Ecommerce.WebAPIs.Services
{
    public interface ICategoryService
    {
        Task<List<CategoryResponseDto>> GetAllCategories();
        Task<CategoryResponseDto?> GetCategoryById(int id);
        Task<CategoryResponseDto> CreateCategory(CategoryRequestDto category);
        Task<CategoryResponseDto> UpdateCategory(int id, CategoryRequestDto updatredCategory);
        Task<bool> DeleteCategory(int id);

    }
}
