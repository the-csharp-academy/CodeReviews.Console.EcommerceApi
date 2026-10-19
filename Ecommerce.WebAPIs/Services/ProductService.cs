using Ecommerce.WebAPIs.DTOs;
using Ecommerce.WebAPIs.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.WebAPIs.Services
{
    public class ProductService : IProductService
    {
        private readonly AppDbContext _dbContext;

        public ProductService(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<ProductResponseDto> CreateProduct(ProductRequestDto product)
        {
            var entity = new ProductEntity
            {
                Name = product.Name,
                Price = product.Price,
                CategoryId = product.CategoryId,
                Quantity = product.Quantity,
            };
            _dbContext.Products.Add(entity);
            await _dbContext.SaveChangesAsync();

            entity = await _dbContext.Products
                .Include(p => p.Category)
                .FirstAsync(p => p.Id == entity.Id);

            return MapToDto(entity);
        }

        public async Task<PagedResultDto<ProductResponseDto>> GetAllProducts(PaginationParams paginationParams)
        {
            var query = _dbContext.Products
                .Include(p => p.Category)
                .Where(p => !p.IsDeleted);

            if (!string.IsNullOrEmpty(paginationParams.SearchTerm))
            {
                query = query.Where(p => p.Name.Contains(paginationParams.SearchTerm));
            }

            if (!string.IsNullOrEmpty(paginationParams.Category))
            {
                query = query.Where(p => p.Category.Name == paginationParams.Category);
            }

            if (paginationParams.SortByPriceAsc == true)
            {
                query = query.OrderBy(p => p.Price);
            }

            else if (paginationParams.SortByPriceDesc == true)
            {
                query = query.OrderByDescending(p => p.Price);
            }

            else if (paginationParams.SortByNameAsc == true)
            {
                query = query.OrderBy(p => p.Name);
            }

            else if (paginationParams.SortByNameDesc == true)
            {
                query = query.OrderByDescending(p => p.Name);
            }

            var totalRecords = await query.CountAsync();

            var products = await query
                .Include(p => p.Category)
                .Skip((paginationParams.PageNumber - 1) * paginationParams.PageSize)
                .Take(paginationParams.PageSize)
                .ToListAsync();

            var dtos = products
                .Select(MapToDto)
                .ToList();

            return new PagedResultDto<ProductResponseDto>
            {
                Items = dtos,
                Page = paginationParams.PageNumber,
                PageSize = paginationParams.PageSize,
                TotalCount = totalRecords
            };
        }

        public async Task<ProductResponseDto?> GetProductById(int productId)
        {
            var entity = await _dbContext.Products.FirstOrDefaultAsync(p => p.Id == productId && !p.IsDeleted);
            if (entity != null)
                return MapToDto(entity);
            return null;
        }

        public async Task<ProductResponseDto?> UpdateProduct(int id, ProductRequestDto updatedProduct)
        {
            var product = await _dbContext.Products.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
            if (product == null)
                throw new ArgumentException($"No such product with id {id}");

            product.Name = updatedProduct.Name;
            // You shouldn't update products prices
            //product.Price = updatedProduct.Price;
            product.CategoryId = updatedProduct.CategoryId;
            product.Quantity = updatedProduct.Quantity;

            await _dbContext.SaveChangesAsync();
            return MapToDto(product);
        }

        public async Task<bool> DeleteProduct(int id)
        {
            var product = await _dbContext.Products.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
            if (product == null)
                return false;
            product.IsDeleted = true;
            await _dbContext.SaveChangesAsync();
            return true;
        }

        private static ProductResponseDto MapToDto(ProductEntity product)
        {
            return new ProductResponseDto
            {
                Id = product.Id,
                Name = product.Name,
                Price = product.Price,
                CategoryId = product.CategoryId,
                CategoryName = product.Category?.Name,
                Quantity = product.Quantity,
            };
        }
    }
}
