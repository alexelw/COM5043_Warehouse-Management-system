namespace Wms.Api.Endpoints;

using Wms.Domain.Enums;

internal sealed record RequiredUserRoleMetadata
{
  public RequiredUserRoleMetadata(IReadOnlyList<UserRole> allowedRoles)
  {
    this.AllowedRoles = allowedRoles;
  }

  public IReadOnlyList<UserRole> AllowedRoles { get; }
}
