namespace Wms.Api.Infrastructure
{
  using System.Collections;
  using System.ComponentModel.DataAnnotations;
  using System.Reflection;
  using System.Text.Json;

  internal static class ApiRequestValidator
  {
    public static void ValidateAndThrow(object model)
    {
      ArgumentNullException.ThrowIfNull(model);

      var errors = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
      ValidateObject(model, errors, prefix: null);

      if (errors.Count == 0)
      {
        return;
      }

      throw new RequestValidationException(errors.ToDictionary(
          pair => pair.Key,
          pair => (IReadOnlyList<string>)pair.Value));
    }

    private static void ValidateObject(
        object model,
        IDictionary<string, List<string>> errors,
        string? prefix)
    {
      var validationContext = new ValidationContext(model);
      var validationResults = new List<ValidationResult>();
      _ = Validator.TryValidateObject(model, validationContext, validationResults, validateAllProperties: true);

      foreach (var validationResult in validationResults)
      {
        var memberNames = validationResult.MemberNames.Any()
            ? validationResult.MemberNames
            : new[] { string.Empty };

        foreach (var memberName in memberNames)
        {
          var field = CombinePath(prefix, memberName);
          AddError(errors, field, validationResult.ErrorMessage ?? "Validation failed.");
        }
      }

      foreach (var property in model.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
      {
        if (property.GetIndexParameters().Length > 0)
        {
          continue;
        }

        var value = property.GetValue(model);
        if (value is null || value is string)
        {
          continue;
        }

        var propertyPath = CombinePath(prefix, property.Name);

        if (value is IEnumerable enumerable)
        {
          var index = 0;
          foreach (var item in enumerable)
          {
            if (item is null || IsSimpleType(item.GetType()))
            {
              index++;
              continue;
            }

            ValidateObject(item, errors, $"{propertyPath}[{index}]");
            index++;
          }

          continue;
        }

        if (!IsSimpleType(value.GetType()))
        {
          ValidateObject(value, errors, propertyPath);
        }
      }
    }

    private static void AddError(IDictionary<string, List<string>> errors, string field, string message)
    {
      var normalizedField = string.IsNullOrWhiteSpace(field) ? string.Empty : ToJsonPath(field);

      if (!errors.TryGetValue(normalizedField, out var messages))
      {
        messages = new List<string>();
        errors[normalizedField] = messages;
      }

      if (!messages.Contains(message, StringComparer.Ordinal))
      {
        messages.Add(message);
      }
    }

    private static string CombinePath(string? prefix, string memberName)
    {
      if (string.IsNullOrWhiteSpace(prefix))
      {
        return memberName;
      }

      if (string.IsNullOrWhiteSpace(memberName))
      {
        return prefix;
      }

      return $"{prefix}.{memberName}";
    }

    private static bool IsSimpleType(Type type)
    {
      var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

      return underlyingType.IsPrimitive ||
             underlyingType.IsEnum ||
             underlyingType == typeof(decimal) ||
             underlyingType == typeof(Guid) ||
             underlyingType == typeof(DateTime) ||
             underlyingType == typeof(DateOnly) ||
             underlyingType == typeof(TimeOnly) ||
             underlyingType == typeof(DateTimeOffset) ||
             underlyingType == typeof(TimeSpan);
    }

    private static string ToJsonPath(string path)
    {
      var segments = path.Split('.', StringSplitOptions.RemoveEmptyEntries);
      return string.Join(".", segments.Select(ToCamelCaseSegment));
    }

    private static string ToCamelCaseSegment(string segment)
    {
      var bracketIndex = segment.IndexOf('[');
      if (bracketIndex < 0)
      {
        return ToCamelCase(segment);
      }

      var property = segment[..bracketIndex];
      var suffix = segment[bracketIndex..];
      return $"{ToCamelCase(property)}{suffix}";
    }

    private static string ToCamelCase(string value)
    {
      return JsonNamingPolicy.CamelCase.ConvertName(value);
    }
  }
}
