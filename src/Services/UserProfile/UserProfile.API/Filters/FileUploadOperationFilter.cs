using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Linq;

namespace UserProfile.API.Filters;

public class FileUploadOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var fileParams = context.MethodInfo.GetParameters()
            .Where(p => p.ParameterType == typeof(IFormFile) || p.ParameterType == typeof(IFormFile))
            .ToList();

        if (!fileParams.Any()) return;

        // Clear existing parameters to avoid duplicates
        var paramsToRemove = operation.Parameters?
            .Where(p => fileParams.Any(fp => fp.Name == p.Name))
            .ToList();

        if (paramsToRemove != null)
        {
            foreach (var param in paramsToRemove)
            {
                operation.Parameters.Remove(param);
            }
        }

        // Create proper multipart/form-data schema
        var uploadFileMediaType = new OpenApiMediaType()
        {
            Schema = new OpenApiSchema()
            {
                Type = "object",
                Properties = new Dictionary<string, OpenApiSchema>(),
                Required = new HashSet<string>()
            }
        };

        // Add all parameters from the method
        foreach (var parameter in context.MethodInfo.GetParameters())
        {
            if (parameter.ParameterType == typeof(IFormFile))
            {
                uploadFileMediaType.Schema.Properties.Add(parameter.Name!, new OpenApiSchema()
                {
                    Type = "string",
                    Format = "binary"
                });
            }
            else if (parameter.ParameterType.IsClass && parameter.ParameterType != typeof(string))
            {
                // Add properties from the request DTO
                var properties = parameter.ParameterType.GetProperties();
                foreach (var prop in properties)
                {
                    uploadFileMediaType.Schema.Properties.Add(
                        char.ToLowerInvariant(prop.Name[0]) + prop.Name.Substring(1),
                        new OpenApiSchema()
                        {
                            Type = prop.PropertyType == typeof(string) ? "string" : 
                                   prop.PropertyType == typeof(int) ? "integer" : "string"
                        });
                }
            }
        }

        operation.RequestBody = new OpenApiRequestBody
        {
            Content = { ["multipart/form-data"] = uploadFileMediaType }
        };
    }
}
