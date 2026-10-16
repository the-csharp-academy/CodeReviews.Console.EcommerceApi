using System.ComponentModel.DataAnnotations;

namespace EcommerceApi.Dtos;

public record ProductResponse(
    int Id,
    string Name,
    string Description,
    decimal Price,
    int StockQuantity,
    int CategoryId,
    string CategoryName,
    DateTime CreatedAtUtc);

public record ProductRequest(
    [Required(ErrorMessage = "Product name is required.")]
    [StringLength(120, ErrorMessage = "Product name cannot exceed 120 characters.")]
    string Name,
    [StringLength(400, ErrorMessage = "Description cannot exceed 400 characters.")]
    string? Description,
    // Range on a decimal property MUST use the Type overload — Range(0.01, 100000) throws at runtime
    // because the attribute cannot convert double bounds to decimal.
    [Range(typeof(decimal), "0.01", "100000", ErrorMessage = "Price must be greater than 0.")]
    decimal Price,
    [Range(0, int.MaxValue, ErrorMessage = "Stock quantity cannot be negative.")]
    int StockQuantity,
    [Range(1, int.MaxValue, ErrorMessage = "A valid category id is required.")]
    int CategoryId);

public class ProductQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int? CategoryId { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string? Search { get; set; }
    public string? SortBy { get; set; }
    public string? SortDir { get; set; }
}
