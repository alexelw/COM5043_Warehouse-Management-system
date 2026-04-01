using Wms.Domain.Enums;

namespace Wms.Application.Finance;

public interface IFinanceService
{
  Task<IReadOnlyList<FinancialTransactionResult>> GetTransactionsAsync(
      FinancialTransactionType? type = null,
      FinancialTransactionStatus? status = null,
      DateTime? from = null,
      DateTime? to = null,
      CancellationToken cancellationToken = default);

  Task<FinancialTransactionResult> GetTransactionAsync(
      Guid transactionId,
      CancellationToken cancellationToken = default);

  Task<FinancialTransactionResult> VoidOrReverseTransactionAsync(
      Guid transactionId,
      VoidOrReverseTransactionRequest request,
      CancellationToken cancellationToken = default);
}
