using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace EcommerceApi.Models;

[Index(nameof(Sku), IsUnique = true)]
public class Product
{
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Sku { get; set; } = string.Empty;

    [Range(typeof(decimal), "0", "79228162514264337593543950335", ErrorMessage = "Price must be 0 or greater.")]
    [Precision(8, 2)]
    public decimal Price { get; set; } = 0;

    [Range(0, int.MaxValue, ErrorMessage = "Stock Quantity must be 0 or greater.")]
    public int StockQuantity { get; set; } = 0;

    public bool IsActive { get; set; } = false;

    public ICollection<ProductSale> ProductSale { get; set; } = [];


    public int? CategoryId { get; set; }
    public Category? Category { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}

