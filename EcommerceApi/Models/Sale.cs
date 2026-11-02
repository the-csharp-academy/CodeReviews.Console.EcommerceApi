using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace EcommerceApi.Models;
    public class Sale
    {
    public int Id { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Quantity must be 0 or greater.")]
    public int Quantity { get; set; }

    [Range(typeof(decimal), "0", "79228162514264337593543950335", ErrorMessage = "Unit Price must be 0 or greater.")]
    [Precision(8,4)]
    public decimal UnitPrice { get; set; }

    [Range(typeof(decimal), "0", "79228162514264337593543950335", ErrorMessage = "Total Price must be 0 or greater.")]
    [Precision(8, 4)]
    public decimal TotalPrice { get; set; }

    public ICollection<ProductSale> ProductSale { get; set; } = [];

    public DateTime SaleDate{ get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}

