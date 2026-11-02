using System.ComponentModel.DataAnnotations;

namespace EcommerceApi.DTO.Category;

public class CategoryDTO
{
    [Required]
    public int? Id { get; set; }

    [MaxLength(50)]
    public string? Name { get; set; }

    [MaxLength(50)]
    public string? Slug { get; set; }   

    public int? ParentId { get; set; }
}