using Wms.Contracts.Common;

namespace Wms.Contracts.PurchaseOrders;

public sealed record PurchaseOrderLineResponse
{
  public Guid ProductId { get; init; }

  public int QuantityOrdered { get; init; }

  public MoneyDto UnitCost { get; init; } = new();
}
