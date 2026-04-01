namespace Wms.Api.Endpoints
{
  using Wms.Contracts.System;

  internal static class HealthEndpoints
  {
    public static void MapHealthEndpoints(this IEndpointRouteBuilder endpoints)
    {
      endpoints.MapGet("/", () => TypedResults.Ok(new HealthResponse
      {
        Name = "WMS API",
        Status = "Running",
      }))
      .ExcludeFromDescription();

      endpoints.MapGet("/api/health", () => TypedResults.Ok(new HealthResponse
      {
        Name = "WMS API",
        Status = "Healthy",
      }))
      .WithTags("System")
      .WithWmsDocs(
          "GetHealth",
          "Health check",
          "Used for monitoring and CI validation.")
      .Produces<HealthResponse>(StatusCodes.Status200OK)
      .ProducesErrorResponses(StatusCodes.Status500InternalServerError);
    }
  }
}
