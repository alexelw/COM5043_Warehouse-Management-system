using Wms.Application.Abstractions;
using Wms.Application.Common.Exceptions;
using Wms.Application.Common.Mappers;
using Wms.Domain.Entities;
using Wms.Domain.Enums;
using Wms.Domain.Repositories;

namespace Wms.Application.Finance;

public sealed class FinanceService : IFinanceService
{
  private readonly ITransactionRepository _transactionRepository;
  private readonly IUnitOfWork _unitOfWork;
  private readonly IClock _clock;

  public FinanceService(
      ITransactionRepository transactionRepository,
      IUnitOfWork unitOfWork,
      IClock clock)
  {
    _transactionRepository = transactionRepository;
    _unitOfWork = unitOfWork;
    _clock = clock;
  }

  public async Task<IReadOnlyList<FinancialTransactionResult>> GetTransactionsAsync(
      FinancialTransactionType? type = null,
      FinancialTransactionStatus? status = null,
      DateTime? from = null,
      DateTime? to = null,
      CancellationToken cancellationToken = default)
  {
    var transactions = await _transactionRepository.ListAsync(
        type,
        status,
        ApplicationMapping.ToDateRange(from, to),
        cancellationToken);

    return transactions.Select(transaction => transaction.ToResult()).ToArray();
  }

  public async Task<FinancialTransactionResult> GetTransactionAsync(
      Guid transactionId,
      CancellationToken cancellationToken = default)
  {
    var transaction = await GetTransactionEntityAsync(transactionId, cancellationToken);
    return transaction.ToResult();
  }

  public async Task<FinancialTransactionResult> VoidOrReverseTransactionAsync(
      Guid transactionId,
      VoidOrReverseTransactionRequest request,
      CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    var transaction = await GetTransactionEntityAsync(transactionId, cancellationToken);

    switch (request.Action)
    {
      case TransactionAction.Void:
        EnsureReason(request.Reason, "Void reason is required.");
        transaction.MarkVoided();
        await _transactionRepository.UpdateAsync(transaction, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return transaction.ToResult();

      case TransactionAction.Reverse:
        var reversal = transaction.CreateReversal(_clock.UtcNow);
        await _transactionRepository.UpdateAsync(transaction, cancellationToken);
        await _transactionRepository.AddAsync(reversal, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return reversal.ToResult();

      default:
        throw new ValidationException("Transaction action is invalid.");
    }
  }

  private async Task<FinancialTransaction> GetTransactionEntityAsync(
      Guid transactionId,
      CancellationToken cancellationToken)
  {
    var transaction = await _transactionRepository.GetByIdAsync(transactionId, cancellationToken);
    if (transaction is null)
    {
      throw new NotFoundException(nameof(FinancialTransaction), transactionId);
    }

    return transaction;
  }

  private static void EnsureReason(string? reason, string message)
  {
    if (string.IsNullOrWhiteSpace(reason))
    {
      throw new ValidationException(message);
    }
  }
}
