namespace Wms.Application.Inventory;

public sealed record StockLevelResult(
    Guid ProductId,
    string Sku,
    string Name,
    int QuantityOnHand);
