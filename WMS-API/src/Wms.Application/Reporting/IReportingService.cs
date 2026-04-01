using Wms.Domain.Enums;

namespace Wms.Application.Reporting;

public interface IReportingService
{
  Task<FinancialReportResult> GenerateFinancialReportAsync(
      DateTime? from = null,
      DateTime? to = null,
      CancellationToken cancellationToken = default);

  Task<ReportExportResult> ExportFinancialReportAsync(
      ExportFinancialReportRequest request,
      CancellationToken cancellationToken = default);

  Task<IReadOnlyList<ReportExportResult>> GetReportExportsAsync(
      ReportType? reportType = null,
      ReportFormat? format = null,
      DateTime? from = null,
      DateTime? to = null,
      CancellationToken cancellationToken = default);
}
