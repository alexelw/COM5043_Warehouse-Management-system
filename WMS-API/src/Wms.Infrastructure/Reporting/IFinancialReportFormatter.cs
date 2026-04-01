using Wms.Application.Reporting;
using Wms.Domain.Enums;

namespace Wms.Infrastructure.Reporting;

internal interface IFinancialReportFormatter
{
  ReportFormat Format { get; }

  string FileExtension { get; }

  string Render(FinancialReportResult report);
}
