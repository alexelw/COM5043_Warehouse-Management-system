using Wms.Contracts.Common;

namespace Wms.Contracts.Finance;

public sealed record FinancialTransactionResponse
{
  public Guid TransactionId { get; init; }

  public string Type { get; init; } = string.Empty;

  public string Status { get; init; } = string.Empty;

  public MoneyDto Amount { get; init; } = new();

  public DateTime OccurredAt { get; init; }

  public string ReferenceType { get; init; } = string.Empty;

  public Guid ReferenceId { get; init; }

  public Guid? ReversalOfTransactionId { get; init; }
}
