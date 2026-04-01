namespace Wms.Contracts.Inventory;

public sealed record StockLevelResponse
{
  public Guid ProductId { get; init; }

  public string Sku { get; init; } = string.Empty;

  public string Name { get; init; } = string.Empty;

  public int QuantityOnHand { get; init; }
}
