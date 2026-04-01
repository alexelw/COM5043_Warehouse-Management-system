namespace Wms.Api.Infrastructure;

using Microsoft.Extensions.Options;
using Wms.Api.Endpoints;
using Wms.Domain.Enums;

internal sealed class WmsRoleAuthorizationMiddleware
{
  internal const string SelectedRoleItemKey = "__Wms.SelectedRole";

  private readonly RequestDelegate _next;
  private readonly WmsRoleOptions _options;

  public WmsRoleAuthorizationMiddleware(
      RequestDelegate next,
      IOptions<WmsRoleOptions> options)
  {
    this._next = next;
    this._options = options.Value;
  }

  public async Task InvokeAsync(HttpContext context)
  {
    var requiredRoles = context.GetEndpoint()?.Metadata.GetMetadata<RequiredUserRoleMetadata>();
    if (requiredRoles is null)
    {
      await this._next(context);
      return;
    }

    var selectedRole = this.ResolveSelectedRole(context);
    context.Items[SelectedRoleItemKey] = selectedRole;

    if (!requiredRoles.AllowedRoles.Contains(selectedRole))
    {
      throw new RoleAccessDeniedException(selectedRole, requiredRoles.AllowedRoles);
    }

    await this._next(context);
  }

  private UserRole ResolveSelectedRole(HttpContext context)
  {
    if (context.Request.Headers.TryGetValue(this._options.HeaderName, out var headerValues))
    {
      var headerValue = headerValues.FirstOrDefault();
      if (!string.IsNullOrWhiteSpace(headerValue))
      {
      return WmsRoleParser.ParseOrThrow(
          headerValue,
          "role",
          $"'{this._options.HeaderName}' must be one of: {WmsRoleParser.GetAllowedRoleValues()}.",
          allowConfiguredDisplayAliases: true);
      }
    }

    if (!string.IsNullOrWhiteSpace(this._options.DefaultRole))
    {
      return WmsRoleParser.ParseOrThrow(
          this._options.DefaultRole,
          "role",
          $"Configured default role must be one of: {WmsRoleParser.GetAllowedRoleValues()}.",
          allowConfiguredDisplayAliases: true);
    }

    throw RequestValidationException.ForSingleError(
        "role",
        $"'{this._options.HeaderName}' header is required. Allowed values: {WmsRoleParser.GetAllowedRoleValues()}.");
  }
}
