namespace Wms.Application.Finance;

public sealed record VoidOrReverseTransactionRequest(
    TransactionAction Action,
    string? Reason);
