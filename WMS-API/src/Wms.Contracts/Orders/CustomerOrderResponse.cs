using Wms.Contracts.Common;

namespace Wms.Contracts.Orders;

public sealed record CustomerOrderResponse
{
  public Guid CustomerOrderId { get; init; }

  public string Status { get; init; } = string.Empty;

  public DateTime CreatedAt { get; init; }

  public IReadOnlyList<CustomerOrderLineResponse> Lines { get; init; } = Array.Empty<CustomerOrderLineResponse>();

  public MoneyDto TotalAmount { get; init; } = new();
}
