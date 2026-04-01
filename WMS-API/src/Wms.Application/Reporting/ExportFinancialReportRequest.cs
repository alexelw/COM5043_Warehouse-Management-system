using Wms.Domain.Enums;

namespace Wms.Application.Reporting;

public sealed record ExportFinancialReportRequest(
    ReportFormat Format,
    DateTime? From,
    DateTime? To);
