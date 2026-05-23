using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Company.API.Swagger;

public sealed class SwaggerIgnoreParameterOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (operation?.Parameters == null || operation.Parameters.Count == 0)
        {
            return;
        }

        var ignoredParameterNames = context.MethodInfo
            .GetParameters()
            .Where(parameter => parameter.GetCustomAttributes(true).OfType<SwaggerIgnoreAttribute>().Any())
            .Select(parameter => parameter.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (ignoredParameterNames.Count == 0)
        {
            return;
        }

        operation.Parameters = operation.Parameters
            .Where(parameter => !ignoredParameterNames.Contains(parameter.Name))
            .ToList();
    }
}
