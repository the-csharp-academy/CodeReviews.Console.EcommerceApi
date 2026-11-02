using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace EcommerceApi.DTO.Sale;

public class SaleCreateDTO
{
    [DefaultValue(0)]
    [Range(0, int.MaxValue, ErrorMessage = "Quantity must be 0 or greater.")]
    public int Quantity { get; set; } = 0;

    [DefaultValue(typeof(decimal), "0")]
    [Range(typeof(decimal), "0", "79228162514264337593543950335", ErrorMessage = "Unit Price must be 0 or greater.")]
    public decimal UnitPrice { get; set; } = 0m;

    [DefaultValue(typeof(decimal), "0")]
    [Range(typeof(decimal), "0", "79228162514264337593543950335", ErrorMessage = "Total Price must be 0 or greater.")]
    public decimal TotalPrice { get; set; } = 0m;

    public ICollection<int>? ProductsIds { get; set; }
}

