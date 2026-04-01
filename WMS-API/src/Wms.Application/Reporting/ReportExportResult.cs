using Wms.Domain.Enums;

namespace Wms.Application.Reporting;

public sealed record ReportExportResult(
    Guid ReportExportId,
    ReportType ReportType,
    ReportFormat Format,
    DateTime GeneratedAt,
    string FilePath,
    DateTime? From,
    DateTime? To);
