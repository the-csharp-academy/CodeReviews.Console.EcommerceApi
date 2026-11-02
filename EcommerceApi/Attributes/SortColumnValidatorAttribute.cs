using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace EcommerceApi.Attributes;

public class SortColumnValidatorAttribute : ValidationAttribute
{
    public Type EntityType { get; set; }

    public SortColumnValidatorAttribute(Type entityType) : base("Value must match an existing column.")
    {
        EntityType = entityType;
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (EntityType != null)
        {
            var strValue = value as string;

            if (!string.IsNullOrEmpty(strValue) && EntityType.GetProperties().Any(p=>p.Name == strValue))
                return ValidationResult.Success;
        }

        return new ValidationResult(ErrorMessage);
    }
}

