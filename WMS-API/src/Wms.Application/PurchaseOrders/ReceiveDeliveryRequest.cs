namespace Wms.Application.PurchaseOrders;

public sealed record ReceiveDeliveryRequest(IReadOnlyList<GoodsReceiptLineInput> Lines);
