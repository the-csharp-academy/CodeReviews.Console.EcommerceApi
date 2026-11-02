using EcommerceApi.Attributes;
using Microsoft.AspNetCore.Http.HttpResults;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace EcommerceApi.DTO;

public class RequestDTO<T> : IValidatableObject
{
    [DefaultValue(0)]
    public int PageIndex { get; set; } = 0;

    [Range(1, 20)]
    [DefaultValue(10)]
    public int PageSize { get; set; } = 10;

    [DefaultValue("Name")]
    public string? SortColumn { get; set; } = "Name";

    [SortOrderValidator]
    [DefaultValue("ASC")]
    public string? SortOrder { get; set; } = "ASC";

    [DefaultValue(null)]
    public string? FilterQuery { get; set; } = null;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var validator = new SortColumnValidatorAttribute(typeof(T));
        var result = validator.GetValidationResult(SortColumn, validationContext);

        var results = new List<ValidationResult>();
        if (result != null && result != ValidationResult.Success)
            results.Add(new ValidationResult(result.ErrorMessage, new[] { nameof(SortColumn) }));

        return results;
    }
}

