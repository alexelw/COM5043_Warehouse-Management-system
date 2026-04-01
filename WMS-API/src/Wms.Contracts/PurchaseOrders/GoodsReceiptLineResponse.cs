namespace Wms.Contracts.PurchaseOrders;

public sealed record GoodsReceiptLineResponse
{
  public Guid ProductId { get; init; }

  public int QuantityReceived { get; init; }
}
