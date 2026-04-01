namespace Wms.Domain.Enums;

/// <summary>
/// Tracks where a purchase order is in its lifecycle.
/// </summary>
public enum PurchaseOrderStatus
{
  Pending = 1,
  PartiallyReceived = 2,
  Completed = 3,
  Cancelled = 4,
}
