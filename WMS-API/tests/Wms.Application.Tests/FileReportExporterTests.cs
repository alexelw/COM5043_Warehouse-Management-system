using System.Text.Json;
using Wms.Application.Common.Models;
using Wms.Application.Reporting;
using Wms.Domain.Enums;
using Wms.Infrastructure.Reporting;

namespace Wms.Application.Tests;

public sealed class FileReportExporterTests : IDisposable
{
  private readonly List<string> createdFiles = [];

  [Fact]
  public async Task ExportFinancialReportAsync_WhenJsonSelected_WritesCamelCasePayload()
  {
    var exporter = new FileReportExporter();
    var report = new FinancialReportResult(
        new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
        new DateTime(2026, 3, 31, 23, 59, 59, DateTimeKind.Utc),
        new MoneyModel(120m, "GBP"),
        new MoneyModel(45m, "GBP"));

    var relativePath = await exporter.ExportFinancialReportAsync(report, ReportFormat.JSON);
    var fullPath = Path.Combine(AppContext.BaseDirectory, relativePath);
    this.createdFiles.Add(fullPath);

    var json = await File.ReadAllTextAsync(fullPath);
    using var document = JsonDocument.Parse(json);
    var root = document.RootElement;

    Assert.True(root.TryGetProperty("from", out _));
    Assert.True(root.TryGetProperty("to", out _));
    Assert.True(root.TryGetProperty("totalSales", out var totalSales));
    Assert.True(root.TryGetProperty("totalExpenses", out _));
    Assert.False(root.TryGetProperty("From", out _));
    Assert.False(root.TryGetProperty("TotalSales", out _));
    Assert.True(totalSales.TryGetProperty("amount", out _));
    Assert.True(totalSales.TryGetProperty("currency", out _));
  }

  [Fact]
  public async Task ExportFinancialReportAsync_WhenTextSelected_WritesFormattedTextReport()
  {
    var exporter = new FileReportExporter();
    var report = new FinancialReportResult(
        new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
        new DateTime(2026, 3, 31, 23, 59, 59, DateTimeKind.Utc),
        new MoneyModel(120m, "GBP"),
        new MoneyModel(45m, "GBP"));

    var relativePath = await exporter.ExportFinancialReportAsync(report, ReportFormat.TXT);
    var fullPath = Path.Combine(AppContext.BaseDirectory, relativePath);
    this.createdFiles.Add(fullPath);

    var content = await File.ReadAllTextAsync(fullPath);

    Assert.Contains("Financial Report", content);
    Assert.Contains("From: 2026-03-01", content);
    Assert.Contains("To: 2026-03-31", content);
    Assert.Contains("Total sales: GBP 120.00", content);
    Assert.Contains("Total expenses: GBP 45.00", content);
  }

  public void Dispose()
  {
    foreach (var path in this.createdFiles.Where(File.Exists))
    {
      File.Delete(path);
    }
  }
}
