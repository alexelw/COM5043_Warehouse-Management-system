namespace Wms.Application.PurchaseOrders;

public sealed record GoodsReceiptLineInput(Guid ProductId, int QuantityReceived);
