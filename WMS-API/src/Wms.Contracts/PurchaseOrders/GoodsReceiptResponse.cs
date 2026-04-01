namespace Wms.Contracts.PurchaseOrders;

public sealed record GoodsReceiptResponse
{
  public Guid GoodsReceiptId { get; init; }

  public Guid PurchaseOrderId { get; init; }

  public DateTime ReceivedAt { get; init; }

  public IReadOnlyList<GoodsReceiptLineResponse> Lines { get; init; } = Array.Empty<GoodsReceiptLineResponse>();
}
