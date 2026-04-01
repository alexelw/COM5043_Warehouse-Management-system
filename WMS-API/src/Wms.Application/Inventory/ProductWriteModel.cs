using Wms.Application.Common.Models;

namespace Wms.Application.Inventory;

public sealed record ProductWriteModel(
    string Sku,
    string Name,
    Guid SupplierId,
    int ReorderThreshold,
    MoneyModel UnitCost);
