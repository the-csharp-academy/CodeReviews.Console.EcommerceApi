using EcommerceApi.Dtos;

namespace EcommerceApi.Services;

public interface IProductService
{
    Task<PagedResult<ProductResponse>> GetPagedAsync(ProductQuery query);
    Task<ProductResponse> GetByIdAsync(int id);
    Task<ProductResponse> CreateAsync(ProductRequest request);
    Task DeleteAsync(int id);
}
