using Wms.Application.Finance;
using Wms.Application.Reporting;
using Wms.Application.Tests.Support;
using Wms.Domain.Entities;
using Wms.Domain.Enums;
using Wms.Domain.ValueObjects;

namespace Wms.Application.Tests;

public class FinanceAndReportingServiceTests
{
  [Fact]
  public async Task VoidOrReverseTransactionAsync_WhenReversing_ReturnsReversalTransaction()
  {
    var transactionRepository = new InMemoryTransactionRepository();
    var unitOfWork = new TrackingUnitOfWork();
    var clock = new FakeClock();
    var service = new FinanceService(transactionRepository, unitOfWork, clock);

    var transaction = new FinancialTransaction(
        FinancialTransactionType.Sale,
        new Money(50m),
        ReferenceType.CustomerOrder,
        Guid.NewGuid(),
        clock.UtcNow);
    transaction.MarkPosted();
    await transactionRepository.AddAsync(transaction);

    var reversal = await service.VoidOrReverseTransactionAsync(
        transaction.TransactionId,
        new VoidOrReverseTransactionRequest(TransactionAction.Reverse, null));

    Assert.Equal(transaction.TransactionId, reversal.ReversalOfTransactionId);
    Assert.Equal(FinancialTransactionStatus.Reversed, transaction.Status);
    Assert.Equal(1, unitOfWork.SaveChangesCalls);
  }

  [Fact]
  public async Task GenerateFinancialReportAsync_UsesPostedSignedTotals()
  {
    var transactionRepository = new InMemoryTransactionRepository();
    var reportExportRepository = new InMemoryReportExportRepository();
    var exporter = new FakeReportExporter();
    var unitOfWork = new TrackingUnitOfWork();
    var service = new ReportingService(
        transactionRepository,
        reportExportRepository,
        exporter,
        unitOfWork);

    var sale = new FinancialTransaction(
        FinancialTransactionType.Sale,
        new Money(100m),
        ReferenceType.CustomerOrder,
        Guid.NewGuid());
    sale.MarkPosted();

    var reversedSale = new FinancialTransaction(
        FinancialTransactionType.Sale,
        new Money(20m),
        ReferenceType.CustomerOrder,
        Guid.NewGuid());
    reversedSale.MarkPosted();
    var reversal = reversedSale.CreateReversal();

    var expense = new FinancialTransaction(
        FinancialTransactionType.PurchaseExpense,
        new Money(40m),
        ReferenceType.PurchaseOrder,
        Guid.NewGuid());
    expense.MarkPosted();

    await transactionRepository.AddAsync(sale);
    await transactionRepository.AddAsync(reversedSale);
    await transactionRepository.AddAsync(reversal);
    await transactionRepository.AddAsync(expense);

    var report = await service.GenerateFinancialReportAsync();

    Assert.Equal(80m, report.TotalSales.Amount);
    Assert.Equal(40m, report.TotalExpenses.Amount);
  }

  [Fact]
  public async Task VoidOrReverseTransactionAsync_WhenVoiding_ReturnsVoidedTransaction()
  {
    var transactionRepository = new InMemoryTransactionRepository();
    var unitOfWork = new TrackingUnitOfWork();
    var clock = new FakeClock();
    var service = new FinanceService(transactionRepository, unitOfWork, clock);

    var transaction = new FinancialTransaction(
        FinancialTransactionType.Sale,
        new Money(50m),
        ReferenceType.CustomerOrder,
        Guid.NewGuid(),
        clock.UtcNow);
    await transactionRepository.AddAsync(transaction);

    var result = await service.VoidOrReverseTransactionAsync(
        transaction.TransactionId,
        new VoidOrReverseTransactionRequest(TransactionAction.Void, "Duplicate entry"));

    Assert.Equal(FinancialTransactionStatus.Voided, result.Status);
    Assert.Equal(FinancialTransactionStatus.Voided, transaction.Status);
    Assert.Equal(1, unitOfWork.SaveChangesCalls);
  }

  [Fact]
  public async Task ExportFinancialReportAsync_PersistsExportRecord()
  {
    var transactionRepository = new InMemoryTransactionRepository();
    var reportExportRepository = new InMemoryReportExportRepository();
    var exporter = new FakeReportExporter
    {
      FilePath = "exports/report.json",
    };
    var unitOfWork = new TrackingUnitOfWork();
    var service = new ReportingService(
        transactionRepository,
        reportExportRepository,
        exporter,
        unitOfWork);

    var result = await service.ExportFinancialReportAsync(new ExportFinancialReportRequest(
        ReportFormat.JSON,
        new DateTime(2026, 1, 1),
        new DateTime(2026, 1, 31)));

    var export = Assert.Single(reportExportRepository.Items);
    Assert.Equal("exports/report.json", result.FilePath);
    Assert.Equal(ReportFormat.JSON, export.Format);
    Assert.Equal("exports/report.json", export.FilePath);
    Assert.Equal(1, unitOfWork.SaveChangesCalls);
  }
}
