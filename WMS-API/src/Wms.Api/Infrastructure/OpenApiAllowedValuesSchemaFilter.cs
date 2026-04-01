namespace Wms.Api.Infrastructure;

using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Wms.Contracts.Common;

internal sealed class OpenApiAllowedValuesSchemaFilter : ISchemaFilter
{
  public void Apply(OpenApiSchema schema, SchemaFilterContext context)
  {
    if (schema.Properties.Count == 0)
    {
      return;
    }

    schema.Required ??= new HashSet<string>();

    foreach (var property in context.Type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
    {
      var jsonPropertyName = ResolveJsonPropertyName(property);
      if (!schema.Properties.TryGetValue(jsonPropertyName, out var propertySchema))
      {
        continue;
      }

      var allowedValuesAttribute = property
          .GetCustomAttributes(typeof(OpenApiAllowedValuesAttribute), inherit: true)
          .OfType<OpenApiAllowedValuesAttribute>()
          .SingleOrDefault();

      if (allowedValuesAttribute is not null)
      {
        propertySchema.Type = "string";
        propertySchema.Enum = allowedValuesAttribute.Values
            .Select(static value => (IOpenApiAny)new OpenApiString(value))
            .ToList();
      }

      if (property.GetCustomAttribute<RequiredAttribute>(inherit: true) is not null)
      {
        schema.Required.Add(jsonPropertyName);
      }
    }
  }

  private static string ResolveJsonPropertyName(PropertyInfo property)
  {
    var explicitName = property.GetCustomAttribute<JsonPropertyNameAttribute>(inherit: true);
    if (explicitName is not null)
    {
      return explicitName.Name;
    }

    return JsonNamingPolicy.CamelCase.ConvertName(property.Name);
  }
}
