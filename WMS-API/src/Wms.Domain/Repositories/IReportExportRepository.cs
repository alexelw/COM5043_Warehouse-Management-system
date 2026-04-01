using Wms.Domain.Entities;
using Wms.Domain.Enums;
using Wms.Domain.ValueObjects;

namespace Wms.Domain.Repositories;

/// <summary>
/// Handles storage and lookup for report export records.
/// </summary>
public interface IReportExportRepository
{
  Task<ReportExport?> GetByIdAsync(Guid reportExportId, CancellationToken cancellationToken = default);

  Task<IReadOnlyList<ReportExport>> ListAsync(
      ReportType? reportType = null,
      ReportFormat? format = null,
      DateRange? generatedWithin = null,
      CancellationToken cancellationToken = default);

  Task AddAsync(ReportExport reportExport, CancellationToken cancellationToken = default);
}
