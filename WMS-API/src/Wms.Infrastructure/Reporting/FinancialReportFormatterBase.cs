using Wms.Application.Reporting;
using Wms.Domain.Enums;

namespace Wms.Infrastructure.Reporting;

internal abstract class FinancialReportFormatterBase : IFinancialReportFormatter
{
  public abstract ReportFormat Format { get; }

  public abstract string FileExtension { get; }

  public abstract string Render(FinancialReportResult report);

  protected static string FormatDate(DateTime? value)
  {
    return value.HasValue ? value.Value.ToString("yyyy-MM-dd") : "All";
  }
}
