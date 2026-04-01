namespace Wms.Contracts.PurchaseOrders;

public sealed record PurchaseOrderResponse
{
  public Guid PurchaseOrderId { get; init; }

  public Guid SupplierId { get; init; }

  public string Status { get; init; } = string.Empty;

  public DateTime CreatedAt { get; init; }

  public IReadOnlyList<PurchaseOrderLineResponse> Lines { get; init; } = Array.Empty<PurchaseOrderLineResponse>();
}
