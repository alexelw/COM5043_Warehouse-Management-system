namespace Wms.Api.Infrastructure
{
  using System.Collections;
  using System.ComponentModel.DataAnnotations;
  using System.Reflection;
  using System.Text.Json;

  internal static class ApiRequestValidator
  {
    private static readonly IReadOnlyList<string> EmptyMemberNames = new[] { string.Empty };

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
      AddValidationErrors(model, errors, prefix);
      ValidateNestedProperties(model, errors, prefix);
    }

    private static void AddValidationErrors(
        object model,
        IDictionary<string, List<string>> errors,
        string? prefix)
    {
      foreach (var validationResult in GetValidationResults(model))
      {
        AddValidationError(validationResult, errors, prefix);
      }
    }

    private static IReadOnlyList<ValidationResult> GetValidationResults(object model)
    {
      var validationContext = new ValidationContext(model);
      var validationResults = new List<ValidationResult>();
      _ = Validator.TryValidateObject(model, validationContext, validationResults, validateAllProperties: true);
      return validationResults;
    }

    private static void AddValidationError(
        ValidationResult validationResult,
        IDictionary<string, List<string>> errors,
        string? prefix)
    {
      var memberNames = validationResult.MemberNames.Any()
          ? validationResult.MemberNames
          : EmptyMemberNames;

      foreach (var memberName in memberNames)
      {
        var field = CombinePath(prefix, memberName);
        AddError(errors, field, validationResult.ErrorMessage ?? "Validation failed.");
      }
    }

    private static void ValidateNestedProperties(
        object model,
        IDictionary<string, List<string>> errors,
        string? prefix)
    {
      foreach (var property in GetValidatableProperties(model.GetType()))
      {
        var value = property.GetValue(model);
        if (ShouldSkipValue(value))
        {
          continue;
        }

        ValidateNestedValue(value!, errors, CombinePath(prefix, property.Name));
      }
    }

    private static IEnumerable<PropertyInfo> GetValidatableProperties(Type type)
    {
      return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
          .Where(static property => property.GetIndexParameters().Length == 0);
    }

    private static bool ShouldSkipValue(object? value)
    {
      return value is null or string;
    }

    private static void ValidateNestedValue(
        object value,
        IDictionary<string, List<string>> errors,
        string propertyPath)
    {
      if (value is IEnumerable enumerable)
      {
        ValidateEnumerableItems(enumerable, errors, propertyPath);
        return;
      }

      if (!IsSimpleType(value.GetType()))
      {
        ValidateObject(value, errors, propertyPath);
      }
    }

    private static void ValidateEnumerableItems(
        IEnumerable enumerable,
        IDictionary<string, List<string>> errors,
        string propertyPath)
    {
      var index = 0;

      foreach (var item in enumerable)
      {
        if (item is not null && !IsSimpleType(item.GetType()))
        {
          ValidateObject(item, errors, $"{propertyPath}[{index}]");
        }

        index++;
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
