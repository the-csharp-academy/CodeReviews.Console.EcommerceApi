using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace EcommerceApi.DTO.Product;

public class ProductCreateDTO
{
    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Sku { get; set; } = string.Empty;

    [DefaultValue(typeof(decimal), "0")]
    [Range(typeof(decimal), "0", "79228162514264337593543950335", ErrorMessage = "Price must be 0 or greater.")]
    public decimal Price { get; set; } = 0m;

    [DefaultValue(0)]
    [Range(0, int.MaxValue, ErrorMessage = "Stock Quantity must be 0 or greater.")]
    public int StockQuantity { get; set; } = 0;

    [DefaultValue(false)]
    public bool IsActive { get; set; } = false;

    public int? CategoryId { get; set; }
    public ICollection<int>? SalesIds { get; set; }
}

