using System.ComponentModel.DataAnnotations;

namespace Ecommerce.WebAPIs.DTOs
{
    public class ProductRequestDto
    {
        [Required(ErrorMessage = "Name is required")]
        public string Name { get; set; }

        public decimal Price { get; set; }

        [Required]
        public int CategoryId { get; set; }

        public int Quantity { get; set; }
    }
}
