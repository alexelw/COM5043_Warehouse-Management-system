using Wms.Application.Common.Models;
using Wms.Domain.Enums;

namespace Wms.Application.Finance;

public sealed record FinancialTransactionResult(
    Guid TransactionId,
    FinancialTransactionType Type,
    FinancialTransactionStatus Status,
    MoneyModel Amount,
    DateTime OccurredAt,
    ReferenceType ReferenceType,
    Guid ReferenceId,
    Guid? ReversalOfTransactionId,
    decimal SignedAmount);
