using Wms.Application.Reporting;
using Wms.Domain.Enums;

namespace Wms.Application.Abstractions;

public interface IReportExporter
{
  Task<string> ExportFinancialReportAsync(
      FinancialReportResult report,
      ReportFormat format,
      CancellationToken cancellationToken = default);
}
