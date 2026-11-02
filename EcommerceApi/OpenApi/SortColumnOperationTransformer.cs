using EcommerceApi.Attributes;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

public class SortColumnOperationTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        foreach (var description in context.Description.ParameterDescriptions)
        {
            var parameterInfo = (description.ParameterDescriptor as ControllerParameterDescriptor)?.ParameterInfo;

            var attributes = parameterInfo?
                .GetCustomAttributes(true)
                .OfType<SortColumnValidatorAttribute>();

            if (attributes == null) continue;

            var parameter = operation.Parameters?
                .FirstOrDefault(p => p.Name == description.Name);

            if (parameter?.Schema == null) continue;

            foreach (var attribute in attributes)
            {
                var pattern = attribute.EntityType
                    .GetProperties()
                    .Select(p => $"^{p.Name}$");

                // .NET 9 (Microsoft.OpenApi 1.x)
                //parameter.Schema.Pattern = string.Join("|", pattern);

                // .NET 10 (Microsoft.OpenApi 2.x): Schema is IOpenApiSchema, so cast:
                if (parameter.Schema is OpenApiSchema schema)
                    schema.Pattern = string.Join("|", pattern);
            }
        }

        return Task.CompletedTask;
    }
}