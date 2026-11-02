using System.ComponentModel.DataAnnotations;

namespace EcommerceApi.DTO.Category;

public class CategoryCreateDTO
{
    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Slug { get; set; } = string.Empty;

    public int? ParentId { get; set; }
}

