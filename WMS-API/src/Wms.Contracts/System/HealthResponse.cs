namespace Wms.Contracts.System;

public sealed record HealthResponse
{
  public string Name { get; init; } = string.Empty;

  public string Status { get; init; } = string.Empty;
}
