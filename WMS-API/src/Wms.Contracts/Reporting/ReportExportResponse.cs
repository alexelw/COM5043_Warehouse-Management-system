namespace Wms.Contracts.Reporting;

public sealed record ReportExportResponse
{
  public Guid ExportId { get; init; }

  public string ReportType { get; init; } = string.Empty;

  public string Format { get; init; } = string.Empty;

  public DateTime GeneratedAt { get; init; }

  public string FilePath { get; init; } = string.Empty;
}
