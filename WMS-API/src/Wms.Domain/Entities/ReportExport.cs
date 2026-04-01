using Wms.Domain.Enums;
using Wms.Domain.Exceptions;
using Wms.Domain.ValueObjects;

namespace Wms.Domain.Entities;

public class ReportExport
{
  private ReportExport()
  {
  }

  public ReportExport(
      ReportType reportType,
      ReportFormat format,
      string filePath,
      DateRange? dateRange = null,
      DateTime? generatedAt = null)
  {
    if (!Enum.IsDefined(reportType))
    {
      throw new DomainRuleViolationException("Report type is invalid.");
    }

    if (!Enum.IsDefined(format))
    {
      throw new DomainRuleViolationException("Report format is invalid.");
    }

    if (string.IsNullOrWhiteSpace(filePath))
    {
      throw new DomainRuleViolationException("File path is required.");
    }

    this.ReportExportId = Guid.NewGuid();
    this.ReportType = reportType;
    this.Format = format;
    this.FilePath = filePath.Trim();
    this.DateRange = dateRange;
    this.GeneratedAt = generatedAt ?? DateTime.UtcNow;
  }

  public Guid ReportExportId { get; private set; }

  public ReportType ReportType { get; private set; }

  public ReportFormat Format { get; private set; }

  public DateTime GeneratedAt { get; private set; }

  public string FilePath { get; private set; } = string.Empty;

  public DateRange? DateRange { get; private set; }
}
