namespace Wms.Api.Infrastructure;

using System.Globalization;

internal static class ApiEndpointHelpers
{
  private static readonly IReadOnlyList<string> InvalidDateRangeErrors =
      new[] { "From date must be on or before To date." };

  public static IReadOnlyList<T> ApplyListOptions<T>(
      IEnumerable<T> source,
      string? sort,
      string? order,
      int page,
      int pageSize,
      string defaultSort,
      bool defaultDescending,
      IReadOnlyDictionary<string, Func<T, IComparable?>> sortSelectors)
  {
    ValidatePagination(page, pageSize);

    var sortField = string.IsNullOrWhiteSpace(sort) ? defaultSort : sort.Trim();
    if (!sortSelectors.TryGetValue(sortField, out var sortSelector))
    {
      throw RequestValidationException.ForSingleError(
          "sort",
          $"Sort must be one of: {string.Join(", ", sortSelectors.Keys.OrderBy(static key => key))}.");
    }

    var descending = ResolveSortDirection(order, defaultDescending);
    var ordered = descending
        ? source.OrderByDescending(sortSelector)
        : source.OrderBy(sortSelector);

    return ordered
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToArray();
  }

  public static TEnum? ParseOptionalEnum<TEnum>(string? value, string fieldName)
      where TEnum : struct, Enum
  {
    if (string.IsNullOrWhiteSpace(value))
    {
      return null;
    }

    if (Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsedValue))
    {
      return parsedValue;
    }

    throw RequestValidationException.ForSingleError(
        fieldName,
        $"{fieldName} must be one of: {string.Join(", ", Enum.GetNames<TEnum>())}.");
  }

  public static TEnum ParseRequiredEnum<TEnum>(string value, string fieldName)
      where TEnum : struct, Enum
  {
    return ParseOptionalEnum<TEnum>(value, fieldName) ??
           throw RequestValidationException.ForSingleError(fieldName, $"{fieldName} is required.");
  }

  public static DateOnly? ParseOptionalDate(string? value, string fieldName)
  {
    if (string.IsNullOrWhiteSpace(value))
    {
      return null;
    }

    if (DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedValue))
    {
      return parsedValue;
    }

    throw RequestValidationException.ForSingleError(fieldName, $"{fieldName} must be a valid date in YYYY-MM-DD format.");
  }

  public static void ValidateDateRange(DateOnly? from, DateOnly? to)
  {
    if (from.HasValue && to.HasValue && from.Value > to.Value)
    {
      throw new RequestValidationException(new Dictionary<string, IReadOnlyList<string>>
      {
        ["from"] = InvalidDateRangeErrors,
        ["to"] = InvalidDateRangeErrors,
      });
    }
  }

  public static DateTime? ToStartOfDayUtc(DateOnly? value)
  {
    return value is null
        ? null
        : DateTime.SpecifyKind(value.Value.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
  }

  public static DateTime? ToEndOfDayUtc(DateOnly? value)
  {
    return value is null
        ? null
        : DateTime.SpecifyKind(value.Value.ToDateTime(TimeOnly.MaxValue), DateTimeKind.Utc);
  }

  private static void ValidatePagination(int page, int pageSize)
  {
    if (page < 1)
    {
      throw RequestValidationException.ForSingleError("page", "Page must be greater than or equal to 1.");
    }

    if (pageSize < 1 || pageSize > 200)
    {
      throw RequestValidationException.ForSingleError("pageSize", "PageSize must be between 1 and 200.");
    }
  }

  private static bool ResolveSortDirection(string? order, bool defaultDescending)
  {
    if (string.IsNullOrWhiteSpace(order))
    {
      return defaultDescending;
    }

    return order.Trim().ToLowerInvariant() switch
    {
      "asc" => false,
      "desc" => true,
      _ => throw RequestValidationException.ForSingleError("order", "Order must be 'asc' or 'desc'."),
    };
  }
}
