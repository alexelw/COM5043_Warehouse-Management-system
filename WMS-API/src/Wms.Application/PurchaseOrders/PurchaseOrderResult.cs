using Wms.Application.Common.Models;
using Wms.Domain.Enums;

namespace Wms.Application.PurchaseOrders;

public sealed record PurchaseOrderResult(
    Guid PurchaseOrderId,
    Guid SupplierId,
    PurchaseOrderStatus Status,
    DateTime CreatedAt,
    IReadOnlyList<PurchaseOrderLineResult> Lines,
    MoneyModel TotalAmount);
