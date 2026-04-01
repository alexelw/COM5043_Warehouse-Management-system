using Wms.Application.Abstractions;
using Wms.Application.Common.Mappers;
using Wms.Application.Common.Models;
using Wms.Domain.Entities;
using Wms.Domain.Enums;
using Wms.Domain.Repositories;
using Wms.Domain.ValueObjects;

namespace Wms.Application.Reporting;

public sealed class ReportingService : IReportingService
{
  private readonly ITransactionRepository _transactionRepository;
  private readonly IReportExportRepository _reportExportRepository;
  private readonly IReportExporter _reportExporter;
  private readonly IUnitOfWork _unitOfWork;

  public ReportingService(
      ITransactionRepository transactionRepository,
      IReportExportRepository reportExportRepository,
      IReportExporter reportExporter,
      IUnitOfWork unitOfWork)
  {
    _transactionRepository = transactionRepository;
    _reportExportRepository = reportExportRepository;
    _reportExporter = reportExporter;
    _unitOfWork = unitOfWork;
  }

  public async Task<FinancialReportResult> GenerateFinancialReportAsync(
      DateTime? from = null,
      DateTime? to = null,
      CancellationToken cancellationToken = default)
  {
    var transactions = await _transactionRepository.ListAsync(
        status: FinancialTransactionStatus.Posted,
        occurredWithin: ApplicationMapping.ToDateRange(from, to),
        cancellationToken: cancellationToken);

    var totalSales = SumSignedAmount(transactions, FinancialTransactionType.Sale);
    var totalExpenses = SumSignedAmount(transactions, FinancialTransactionType.PurchaseExpense);

    return new FinancialReportResult(
        from,
        to,
        CreateMoneyModel(totalSales),
        CreateMoneyModel(totalExpenses));
  }

  public async Task<ReportExportResult> ExportFinancialReportAsync(
      ExportFinancialReportRequest request,
      CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    var report = await GenerateFinancialReportAsync(request.From, request.To, cancellationToken);
    var filePath = await _reportExporter.ExportFinancialReportAsync(report, request.Format, cancellationToken);

    var export = new ReportExport(
        ReportType.FinancialSummary,
        request.Format,
        filePath,
        ApplicationMapping.ToDateRange(request.From, request.To));

    await _reportExportRepository.AddAsync(export, cancellationToken);
    await _unitOfWork.SaveChangesAsync(cancellationToken);

    return export.ToResult();
  }

  public async Task<IReadOnlyList<ReportExportResult>> GetReportExportsAsync(
      ReportType? reportType = null,
      ReportFormat? format = null,
      DateTime? from = null,
      DateTime? to = null,
      CancellationToken cancellationToken = default)
  {
    var exports = await _reportExportRepository.ListAsync(
        reportType,
        format,
        ApplicationMapping.ToDateRange(from, to),
        cancellationToken);

    return exports.Select(exportRecord => exportRecord.ToResult()).ToArray();
  }

  private static decimal SumSignedAmount(
      IEnumerable<FinancialTransaction> transactions,
      FinancialTransactionType type)
  {
    return decimal.Round(
        transactions
            .Where(transaction => transaction.Type == type)
            .Sum(transaction => transaction.SignedAmount),
        2,
        MidpointRounding.AwayFromZero);
  }

  private static MoneyModel CreateMoneyModel(decimal amount)
  {
    return new MoneyModel(amount, Money.GbpCurrencyCode);
  }
}
