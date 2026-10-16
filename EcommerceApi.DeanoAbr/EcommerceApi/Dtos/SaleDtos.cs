using System.ComponentModel.DataAnnotations;

namespace EcommerceApi.Dtos;

public record SaleLineRequest(
    [Range(1, int.MaxValue, ErrorMessage = "A valid product id is required.")]
    int ProductId,
    [Range(1, 1000, ErrorMessage = "Quantity must be at least 1.")]
    int Quantity);

public record SaleRequest(
    [Required(ErrorMessage = "Customer name is required.")]
    [StringLength(120, ErrorMessage = "Customer name cannot exceed 120 characters.")]
    string CustomerName,
    [Required(ErrorMessage = "A sale must contain at least one item.")]
    [MinLength(1, ErrorMessage = "A sale must contain at least one item.")]
    List<SaleLineRequest> Items);

public record SaleLineResponse(
    int ProductId, string ProductName, int Quantity, decimal UnitPrice, decimal LineTotal);

public record SaleResponse(
    int Id,
    string CustomerName,
    DateTime SoldAtUtc,
    decimal Total,
    IReadOnlyList<SaleLineResponse> Items);

public class SalesQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Customer { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public string? SortBy { get; set; }
    public string? SortDir { get; set; }
}
