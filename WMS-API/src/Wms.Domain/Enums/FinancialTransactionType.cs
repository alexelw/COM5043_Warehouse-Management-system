namespace Wms.Domain.Enums;

/// <summary>
/// Identifies the kind of financial entry created by a warehouse workflow.
/// </summary>
public enum FinancialTransactionType
{
  Sale = 1,
  PurchaseExpense = 2,
  StockAdjustment = 3,
}
