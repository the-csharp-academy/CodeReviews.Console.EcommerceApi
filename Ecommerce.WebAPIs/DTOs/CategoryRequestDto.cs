using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Ecommerce.WebAPIs.DTOs
{
    public class CategoryRequestDto
    {
        [Required(ErrorMessage = "Category name is required")]
        public string Name { get; set; }
    }
}
