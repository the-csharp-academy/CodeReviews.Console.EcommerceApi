using System.ComponentModel.DataAnnotations;

namespace EcommerceApi.DTO.Sale;

public class SaleDTO
{
    [Required]
    public int? Id { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Quantity must be 0 or greater.")]
    public int? Quantity { get; set; }
  
    [Range(typeof(decimal), "0", "79228162514264337593543950335", ErrorMessage = "Unit Price must be 0 or greater.")]
    public decimal? UnitPrice { get; set; }

    [Range(typeof(decimal), "0", "79228162514264337593543950335", ErrorMessage = "Total Price must be 0 or greater.")]
    public decimal? TotalPrice { get; set; }

    public ICollection<int>? ProductsIds { get; set; }
}