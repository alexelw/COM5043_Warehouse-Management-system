namespace Wms.Application.PurchaseOrders;

public sealed record CreatePurchaseOrderRequest(
    Guid SupplierId,
    IReadOnlyList<PurchaseOrderLineInput> Lines);
