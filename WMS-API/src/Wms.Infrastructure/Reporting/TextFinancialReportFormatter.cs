using Wms.Application.Reporting;
using Wms.Domain.Enums;

namespace Wms.Infrastructure.Reporting;

internal sealed class TextFinancialReportFormatter : FinancialReportFormatterBase
{
  public override ReportFormat Format => ReportFormat.TXT;

  public override string FileExtension => "txt";

  public override string Render(FinancialReportResult report)
  {
    ArgumentNullException.ThrowIfNull(report);

    return string.Join(
        Environment.NewLine,
        new[]
        {
            "Financial Report",
            $"From: {FormatDate(report.From)}",
            $"To: {FormatDate(report.To)}",
            $"Total sales: {report.TotalSales.Currency} {report.TotalSales.Amount:0.00}",
            $"Total expenses: {report.TotalExpenses.Currency} {report.TotalExpenses.Amount:0.00}",
        });
  }
}
