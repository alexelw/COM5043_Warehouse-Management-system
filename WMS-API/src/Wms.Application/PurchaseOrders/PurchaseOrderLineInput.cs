using Wms.Application.Common.Models;

namespace Wms.Application.PurchaseOrders;

public sealed record PurchaseOrderLineInput(
    Guid ProductId,
    int Quantity,
    MoneyModel UnitCost);
