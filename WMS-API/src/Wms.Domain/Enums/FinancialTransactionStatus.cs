namespace Wms.Domain.Enums;

/// <summary>
/// Tracks where a financial transaction is in its lifecycle.
/// </summary>
public enum FinancialTransactionStatus
{
  Pending = 1,
  Posted = 2,
  Voided = 3,
  Reversed = 4,
}
