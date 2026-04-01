using Wms.Domain.Enums;
using Wms.Domain.Exceptions;

namespace Wms.Domain.Entities;

public class User
{
  private User()
  {
  }

  public User(string displayName, UserRole role)
  {
    this.UserId = Guid.NewGuid();
    this.ChangeDisplayName(displayName);
    this.ChangeRole(role);
  }

  public Guid UserId { get; private set; }

  public string DisplayName { get; private set; } = string.Empty;

  public UserRole Role { get; private set; }

  public void ChangeDisplayName(string displayName)
  {
    this.DisplayName = NormalizeRequired(displayName, "Display name is required.");
  }

  public void ChangeRole(UserRole role)
  {
    if (!Enum.IsDefined(role))
    {
      throw new DomainRuleViolationException("User role is invalid.");
    }

    this.Role = role;
  }

  private static string NormalizeRequired(string value, string message)
  {
    if (string.IsNullOrWhiteSpace(value))
    {
      throw new DomainRuleViolationException(message);
    }

    return value.Trim();
  }
}
