using System.ComponentModel.DataAnnotations;

namespace EcommerceApi.DTO.Product;

public class SaleDTO
{
    [Required]
    public int? Id { get; set; }

    [MaxLength(50)]
    public string? Name { get; set; }

    [MaxLength(50)]
    public string? Sku { get; set; }

    [Range(typeof(decimal), "0", "79228162514264337593543950335", ErrorMessage = "Price must be 0 or greater.")]
    public decimal? Price { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Stock Quantity must be 0 or greater.")]
    public int? StockQuantity { get; set; }

    public bool? IsActive { get; set; }

    public int? CategoryId { get; set; }
    public ICollection<int>? SalesIds { get; set; }
}