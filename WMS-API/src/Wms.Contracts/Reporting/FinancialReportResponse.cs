using Wms.Contracts.Common;

namespace Wms.Contracts.Reporting;

public sealed record FinancialReportResponse
{
  public DateOnly? From { get; init; }

  public DateOnly? To { get; init; }

  public MoneyDto TotalSales { get; init; } = new();

  public MoneyDto TotalExpenses { get; init; } = new();
}
