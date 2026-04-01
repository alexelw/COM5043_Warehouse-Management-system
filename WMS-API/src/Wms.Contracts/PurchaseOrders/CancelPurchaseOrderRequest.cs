namespace Wms.Contracts.PurchaseOrders;

public sealed record CancelPurchaseOrderRequest
{
  [Required]
  public string Reason { get; init; } = string.Empty;
}
