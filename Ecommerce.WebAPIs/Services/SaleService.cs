using Ecommerce.WebAPIs.DTOs;
using Ecommerce.WebAPIs.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.WebAPIs.Services
{
    public class SaleService : ISaleService
    {
        private readonly AppDbContext _dbContext;

        public SaleService(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<SaleResponseDto> CreateSale(SaleRequestDto sale)
        {
            var productIds = sale.Products;
            var dtos = new List<ProductResponseDto>();
            var products = await _dbContext.Products
                .Where(p => productIds.Contains(p.Id) && !p.IsDeleted)
                .Include(p => p.Category)
                .ToListAsync();

            if (products.Count != productIds.Distinct().Count())
                throw new ArgumentException("Some products were not found");

            foreach (var product in products)
            {
                if (product.Quantity != 0)
                    product.Quantity--;
                else throw new ArgumentException($"Unable to do a sale: quantity or {product.Name} is 0");
                var dto = new ProductResponseDto
                {
                    Id = product.Id,
                    Name = product.Name,
                    Price = product.Price,
                    CategoryId = product.CategoryId,
                    Quantity = product.Quantity
                };
                dtos.Add(dto);
            }

            var entity = new SaleEntity
            {
                Products = products,
                SaleTime = DateTime.Now,
            };
            _dbContext.Sales.Add(entity);
            await _dbContext.SaveChangesAsync();

            var response = new SaleResponseDto
            {
                Id = entity.Id,
                Products = dtos,
                SaleTime = entity.SaleTime
            };
            return response;
        }

        public async Task<PagedResultDto<SaleResponseDto>> GetAllSales(PaginationParams paginationParams)
        {
            var query = _dbContext.Sales
                .Where(s => !s.IsDeleted)
                .OrderBy(s => s.SaleTime)
                .Include(s => s.Products)
                .ThenInclude(p => p.Category)
                .AsNoTracking();

            var totalCount = await query.CountAsync();

            var sales = await query
                .Skip((paginationParams.PageNumber - 1) * paginationParams.PageSize)
                .Take(paginationParams.PageSize)
                .ToListAsync();

            var dtos = sales.Select(s => new SaleResponseDto
            {
                Id = s.Id,
                SaleTime = s.SaleTime,
                Products = s.Products.Select(p => new ProductResponseDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Price = p.Price,
                    CategoryId = p.CategoryId,
                    CategoryName = p.Category.Name,
                    Quantity = p.Quantity
                }).ToList()
            }).ToList();

            return new PagedResultDto<SaleResponseDto>
            {
                Items = dtos,
                Page = paginationParams.PageNumber,
                PageSize = paginationParams.PageSize,
                TotalCount = totalCount
            };
        }

        public async Task<SaleResponseDto?> GetSalesById(int id)
        {
            var entity = await _dbContext.Sales
                .Where(s => !s.IsDeleted)
                .Include(s => s.Products)
                .ThenInclude(p => p.Category)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (entity == null)
                return null;
            return new SaleResponseDto
            {
                Id = entity.Id,
                Products = entity.Products.Select(p => new ProductResponseDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Price = p.Price,
                    CategoryId = p.CategoryId,
                    CategoryName = p.Category.Name,
                    Quantity = p.Quantity
                }).ToList()
            };
        }

        //public Task<SaleResponseDto> UpdateSale(int id, SaleRequestDto updatedSale)
        //{
        //    throw new NotImplementedException();
        //}

        public async Task<bool> DeleteSale(int id)
        {
            var entity = await _dbContext.Sales
                .Where(s => !s.IsDeleted)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (entity == null)
                return false;

            entity.IsDeleted = true;
            _dbContext.Update(entity);
            await _dbContext.SaveChangesAsync();
            return true;
        }
    }
}
