namespace Wms.Domain.Enums;

/// <summary>
/// Tracks where a customer order is in its lifecycle.
/// </summary>
public enum CustomerOrderStatus
{
  Draft = 1,
  Confirmed = 2,
  Cancelled = 3,
}
