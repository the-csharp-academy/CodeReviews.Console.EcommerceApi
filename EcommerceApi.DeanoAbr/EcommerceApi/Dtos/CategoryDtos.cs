using System.ComponentModel.DataAnnotations;

namespace EcommerceApi.Dtos;

public record CategoryResponse(int Id, string Name, string Description, int ProductCount);

// NOTE: a bare attribute targets the record's constructor parameter. Never write [property: Required]
// here — MVC throws at binding time and every request with a body returns a 500.
// StringLength deliberately has no MinimumLength: paired with Required it would emit a second message
// for the same empty value. Whitespace-only input is rejected in the service, with the same wording.
public record CategoryRequest(
    [Required(ErrorMessage = "Category name is required.")]
    [StringLength(80, ErrorMessage = "Category name cannot exceed 80 characters.")]
    string Name,
    [StringLength(400, ErrorMessage = "Description cannot exceed 400 characters.")]
    string? Description);
