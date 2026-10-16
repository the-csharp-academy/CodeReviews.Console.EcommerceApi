using EcommerceApi.Dtos;

namespace EcommerceApi.Services;

public interface ISaleService
{
    Task<PagedResult<SaleResponse>> GetPagedAsync(SalesQuery query);
    Task<SaleResponse> GetByIdAsync(int id);
    Task<SaleResponse> CreateAsync(SaleRequest request);
    Task DeleteAsync(int id);
}
