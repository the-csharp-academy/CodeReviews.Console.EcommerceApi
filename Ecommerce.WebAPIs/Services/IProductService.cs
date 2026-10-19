using Ecommerce.WebAPIs.DTOs;
using Ecommerce.WebAPIs.Entities;

namespace Ecommerce.WebAPIs.Services
{
    public interface IProductService
    {
        Task<PagedResultDto<ProductResponseDto>> GetAllProducts(PaginationParams paginationParams);
        Task<ProductResponseDto?> GetProductById(int productId);
        Task<ProductResponseDto> CreateProduct(ProductRequestDto product);
        Task<ProductResponseDto?> UpdateProduct(int id, ProductRequestDto updatedProduct);
        Task<bool> DeleteProduct(int id);
    }
}
