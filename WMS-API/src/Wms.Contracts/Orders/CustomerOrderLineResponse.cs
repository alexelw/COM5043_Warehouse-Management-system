using Wms.Contracts.Common;

namespace Wms.Contracts.Orders;

public sealed record CustomerOrderLineResponse
{
  public Guid ProductId { get; init; }

  public int Quantity { get; init; }

  public MoneyDto UnitPrice { get; init; } = new();
}
