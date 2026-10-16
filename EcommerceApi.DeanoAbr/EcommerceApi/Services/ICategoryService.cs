using EcommerceApi.Dtos;

namespace EcommerceApi.Services;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryResponse>> GetAllAsync();
    Task<CategoryResponse> GetByIdAsync(int id);
    Task<CategoryResponse> CreateAsync(CategoryRequest request);
    Task<CategoryResponse> UpdateAsync(int id, CategoryRequest request);
    Task DeleteAsync(int id);
}
