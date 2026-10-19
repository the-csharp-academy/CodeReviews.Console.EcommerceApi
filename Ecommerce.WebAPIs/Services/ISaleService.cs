using Ecommerce.WebAPIs.DTOs;

namespace Ecommerce.WebAPIs.Services
{
    public interface ISaleService
    {
        Task<SaleResponseDto> CreateSale(SaleRequestDto sale);
        Task<PagedResultDto<SaleResponseDto>> GetAllSales(PaginationParams paginationParams);
        Task<SaleResponseDto?> GetSalesById(int id);
        //Task<SaleResponseDto> UpdateSale(int id, SaleRequestDto updatedSale);
        Task<bool> DeleteSale(int id);
    }
}
