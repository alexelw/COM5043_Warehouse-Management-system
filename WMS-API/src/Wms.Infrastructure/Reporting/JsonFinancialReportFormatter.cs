using System.Text.Json;
using Wms.Application.Reporting;
using Wms.Domain.Enums;

namespace Wms.Infrastructure.Reporting;

internal sealed class JsonFinancialReportFormatter : FinancialReportFormatterBase
{
  private static readonly JsonSerializerOptions ExportJsonOptions = new(JsonSerializerDefaults.Web)
  {
    WriteIndented = true,
  };

  public override ReportFormat Format => ReportFormat.JSON;

  public override string FileExtension => "json";

  public override string Render(FinancialReportResult report)
  {
    ArgumentNullException.ThrowIfNull(report);

    return JsonSerializer.Serialize(
        new
        {
          report.From,
          report.To,
          report.TotalSales,
          report.TotalExpenses,
        },
        ExportJsonOptions);
  }
}
