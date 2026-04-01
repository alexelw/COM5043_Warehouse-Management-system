namespace Wms.Api.Infrastructure;

using Wms.Domain.Enums;

internal static class WmsRoleParser
{
  public static string GetAllowedRoleValues()
  {
    return string.Join(", ", Enum.GetValues<UserRole>().Select(ToHeaderValue));
  }

  public static string ToHeaderValue(UserRole role)
  {
    return role.ToString();
  }

  public static string ToDisplayName(UserRole role)
  {
    return role switch
    {
      UserRole.WarehouseStaff => "Warehouse Staff",
      UserRole.Manager => "Manager",
      UserRole.Administrator => "Administrator",
      _ => role.ToString(),
    };
  }

  public static UserRole ParseOrThrow(
      string? value,
      string fieldName,
      string invalidMessage,
      bool allowConfiguredDisplayAliases)
  {
    if (TryParse(value, out var parsedRole, allowConfiguredDisplayAliases))
    {
      return parsedRole;
    }

    throw RequestValidationException.ForSingleError(fieldName, invalidMessage);
  }

  private static bool TryParse(
      string? value,
      out UserRole role,
      bool allowConfiguredDisplayAliases)
  {
    role = default;
    if (string.IsNullOrWhiteSpace(value))
    {
      return false;
    }

    var normalizedValue = Normalize(value);
    foreach (var candidate in Enum.GetValues<UserRole>())
    {
      if (Normalize(candidate.ToString()) == normalizedValue)
      {
        role = candidate;
        return true;
      }

      if (allowConfiguredDisplayAliases && Normalize(ToDisplayName(candidate)) == normalizedValue)
      {
        role = candidate;
        return true;
      }
    }

    return false;
  }

  private static string Normalize(string value)
  {
    return new string(value.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
  }
}
