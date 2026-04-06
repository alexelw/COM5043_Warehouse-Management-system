using Wms.Application.Abstractions;
using Wms.Application.Reporting;
using Wms.Domain.Enums;

namespace Wms.Infrastructure.Reporting;

public sealed class FileReportExporter : IReportExporter
{
  private readonly Dictionary<ReportFormat, IFinancialReportFormatter> _formatters;

  public FileReportExporter()
      : this(new IFinancialReportFormatter[]
      {
        new TextFinancialReportFormatter(),
        new JsonFinancialReportFormatter(),
      })
  {
  }

  internal FileReportExporter(IEnumerable<IFinancialReportFormatter> formatters)
  {
    ArgumentNullException.ThrowIfNull(formatters);

    var formatterMap = new Dictionary<ReportFormat, IFinancialReportFormatter>();
    foreach (var formatter in formatters)
    {
      ArgumentNullException.ThrowIfNull(formatter);

      if (!formatterMap.TryAdd(formatter.Format, formatter))
      {
        throw new InvalidOperationException($"A formatter for '{formatter.Format}' is already registered.");
      }
    }

    if (formatterMap.Count == 0)
    {
      throw new InvalidOperationException("At least one financial report formatter is required.");
    }

    this._formatters = formatterMap;
  }

  public async Task<string> ExportFinancialReportAsync(
      FinancialReportResult report,
      ReportFormat format,
      CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(report);

    if (!this._formatters.TryGetValue(format, out var formatter))
    {
      throw new InvalidOperationException($"Unsupported export format '{format}'.");
    }

    var fileName = $"financial-report-{DateTime.UtcNow:yyyyMMddHHmmssfff}.{formatter.FileExtension}";
    var relativePath = Path.Combine("exports", fileName);
    var fullPath = Path.Combine(AppContext.BaseDirectory, relativePath);

    Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

    var content = formatter.Render(report);

    await File.WriteAllTextAsync(fullPath, content, cancellationToken);
    return relativePath;
  }
}
