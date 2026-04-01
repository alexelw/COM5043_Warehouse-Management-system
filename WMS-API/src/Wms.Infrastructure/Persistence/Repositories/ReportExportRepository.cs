using Microsoft.EntityFrameworkCore;
using Wms.Domain.Entities;
using Wms.Domain.Enums;
using Wms.Domain.Repositories;
using Wms.Domain.ValueObjects;

namespace Wms.Infrastructure.Persistence.Repositories;

public sealed class ReportExportRepository : IReportExportRepository
{
  private readonly WmsDbContext _dbContext;

  public ReportExportRepository(WmsDbContext dbContext)
  {
    _dbContext = dbContext;
  }

  public Task<ReportExport?> GetByIdAsync(Guid reportExportId, CancellationToken cancellationToken = default)
  {
    return _dbContext.ReportExports.SingleOrDefaultAsync(
        reportExport => reportExport.ReportExportId == reportExportId,
        cancellationToken);
  }

  public async Task<IReadOnlyList<ReportExport>> ListAsync(
      ReportType? reportType = null,
      ReportFormat? format = null,
      DateRange? generatedWithin = null,
      CancellationToken cancellationToken = default)
  {
    IQueryable<ReportExport> query = _dbContext.ReportExports.AsNoTracking();

    if (reportType.HasValue)
    {
      query = query.Where(reportExport => reportExport.ReportType == reportType.Value);
    }

    if (format.HasValue)
    {
      query = query.Where(reportExport => reportExport.Format == format.Value);
    }

    if (generatedWithin is not null)
    {
      query = query.Where(reportExport =>
          reportExport.GeneratedAt >= generatedWithin.From &&
          reportExport.GeneratedAt <= generatedWithin.To);
    }

    return await query
        .OrderByDescending(reportExport => reportExport.GeneratedAt)
        .ToArrayAsync(cancellationToken);
  }

  public Task AddAsync(ReportExport reportExport, CancellationToken cancellationToken = default)
  {
    return _dbContext.ReportExports.AddAsync(reportExport, cancellationToken).AsTask();
  }
}
