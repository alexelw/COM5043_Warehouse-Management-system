using Wms.Application.Common.Models;

namespace Wms.Application.PurchaseOrders;

public sealed record PurchaseOrderLineResult(
    Guid ProductId,
    int QuantityOrdered,
    MoneyModel UnitCost);
