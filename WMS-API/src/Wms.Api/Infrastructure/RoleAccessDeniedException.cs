namespace Wms.Api.Infrastructure;

using Wms.Domain.Enums;

public sealed class RoleAccessDeniedException : Exception
{
  public RoleAccessDeniedException(UserRole selectedRole, IReadOnlyList<UserRole> allowedRoles)
      : base(CreateMessage(selectedRole, allowedRoles))
  {
    this.SelectedRole = selectedRole;
    this.AllowedRoles = allowedRoles;
  }

  public UserRole SelectedRole { get; }

  public IReadOnlyList<UserRole> AllowedRoles { get; }

  private static string CreateMessage(UserRole selectedRole, IReadOnlyList<UserRole> allowedRoles)
  {
    var allowedRoleNames = string.Join(", ", allowedRoles.Select(WmsRoleParser.ToDisplayName));
    return $"Role '{WmsRoleParser.ToDisplayName(selectedRole)}' cannot perform this operation. Allowed roles: {allowedRoleNames}.";
  }
}
