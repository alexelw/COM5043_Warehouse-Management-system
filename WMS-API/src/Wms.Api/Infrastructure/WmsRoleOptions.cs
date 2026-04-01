namespace Wms.Api.Infrastructure;

internal sealed class WmsRoleOptions
{
  public string HeaderName { get; set; } = "X-Wms-Role";

  public string? DefaultRole { get; set; }
}
