using Wms.Application.Common.Models;

namespace Wms.Application.Reporting;

public sealed record FinancialReportResult(
    DateTime? From,
    DateTime? To,
    MoneyModel TotalSales,
    MoneyModel TotalExpenses);
