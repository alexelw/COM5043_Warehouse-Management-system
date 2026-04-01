namespace Wms.Api.Endpoints;

using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Wms.Api.Infrastructure;
using Wms.Contracts.Common;
using Wms.Domain.Enums;

internal static class SwaggerMetadataExtensions
{
  public static RouteHandlerBuilder WithWmsDocs(
      this RouteHandlerBuilder builder,
      string operationName,
      string summary,
      string description)
  {
    return builder
        .WithName(operationName)
        .WithSummary(summary)
        .WithDescription(description);
  }

  public static RouteHandlerBuilder ProducesErrorResponses(
      this RouteHandlerBuilder builder,
      params int[] statusCodes)
  {
    foreach (var statusCode in statusCodes.Distinct())
    {
      builder.Produces<ErrorResponse>(statusCode);
    }

    return builder;
  }

  public static RouteHandlerBuilder RequireWmsRole(
      this RouteHandlerBuilder builder,
      params UserRole[] allowedRoles)
  {
    ArgumentNullException.ThrowIfNull(builder);

    var distinctRoles = allowedRoles.Distinct().ToArray();
    if (distinctRoles.Length == 0)
    {
      throw new ArgumentException("At least one allowed role is required.", nameof(allowedRoles));
    }

    return builder
        .WithMetadata(new RequiredUserRoleMetadata(distinctRoles))
        .ProducesErrorResponses(StatusCodes.Status400BadRequest, StatusCodes.Status403Forbidden);
  }
}
