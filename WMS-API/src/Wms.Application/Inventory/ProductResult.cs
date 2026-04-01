using Wms.Application.Common.Models;

namespace Wms.Application.Inventory;

public sealed record ProductResult(
    Guid ProductId,
    string Sku,
    string Name,
    Guid SupplierId,
    int ReorderThreshold,
    int QuantityOnHand,
    MoneyModel UnitCost);
