using Wms.Contracts.Common;

namespace Wms.Contracts.Inventory;

public sealed record ProductResponse
{
  public Guid ProductId { get; init; }

  public string Sku { get; init; } = string.Empty;

  public string Name { get; init; } = string.Empty;

  public Guid SupplierId { get; init; }

  public int ReorderThreshold { get; init; }

  public int QuantityOnHand { get; init; }

  public MoneyDto UnitCost { get; init; } = new();
}
