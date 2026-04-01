namespace Wms.Contracts.Common;

public sealed record ErrorResponse
{
  public string TraceId { get; init; } = string.Empty;

  public string Code { get; init; } = string.Empty;

  public string Message { get; init; } = string.Empty;

  public IReadOnlyDictionary<string, IReadOnlyList<string>> Errors { get; init; } =
      new Dictionary<string, IReadOnlyList<string>>();
}
