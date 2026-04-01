namespace Wms.Contracts.Orders;

public sealed record CancelCustomerOrderRequest
{
  [Required]
  public string Reason { get; init; } = string.Empty;
}
