using Wms.Domain.Entities;
using Wms.Domain.Enums;
using Wms.Domain.ValueObjects;

namespace Wms.Domain.Repositories;

/// <summary>
/// Handles storage and lookup for financial transactions.
/// </summary>
public interface ITransactionRepository
{
  Task<FinancialTransaction?> GetByIdAsync(Guid transactionId, CancellationToken cancellationToken = default);

  Task<IReadOnlyList<FinancialTransaction>> ListAsync(
      FinancialTransactionType? type = null,
      FinancialTransactionStatus? status = null,
      DateRange? occurredWithin = null,
      CancellationToken cancellationToken = default);

  Task<IReadOnlyList<FinancialTransaction>> ListByReferenceAsync(
      ReferenceType referenceType,
      Guid referenceId,
      CancellationToken cancellationToken = default);

  Task AddAsync(FinancialTransaction transaction, CancellationToken cancellationToken = default);

  Task UpdateAsync(FinancialTransaction transaction, CancellationToken cancellationToken = default);
}
