namespace Wms.Application.PurchaseOrders;

public sealed record GoodsReceiptResult(
    Guid GoodsReceiptId,
    Guid PurchaseOrderId,
    DateTime ReceivedAt,
    IReadOnlyList<GoodsReceiptLineResult> Lines);
