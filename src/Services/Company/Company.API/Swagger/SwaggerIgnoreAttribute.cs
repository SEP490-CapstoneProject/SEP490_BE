using System;

namespace Company.API.Swagger;

[AttributeUsage(AttributeTargets.Parameter)]
public sealed class SwaggerIgnoreAttribute : Attribute
{
}
