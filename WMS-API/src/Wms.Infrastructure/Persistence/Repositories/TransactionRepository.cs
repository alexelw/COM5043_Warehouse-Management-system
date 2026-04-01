using Microsoft.EntityFrameworkCore;
using Wms.Domain.Entities;
using Wms.Domain.Enums;
using Wms.Domain.Repositories;
using Wms.Domain.ValueObjects;

namespace Wms.Infrastructure.Persistence.Repositories;

public sealed class TransactionRepository : ITransactionRepository
{
  private readonly WmsDbContext _dbContext;

  public TransactionRepository(WmsDbContext dbContext)
  {
    _dbContext = dbContext;
  }

  public Task<FinancialTransaction?> GetByIdAsync(Guid transactionId, CancellationToken cancellationToken = default)
  {
    return _dbContext.FinancialTransactions.SingleOrDefaultAsync(
        transaction => transaction.TransactionId == transactionId,
        cancellationToken);
  }

  public async Task<IReadOnlyList<FinancialTransaction>> ListAsync(
      FinancialTransactionType? type = null,
      FinancialTransactionStatus? status = null,
      DateRange? occurredWithin = null,
      CancellationToken cancellationToken = default)
  {
    IQueryable<FinancialTransaction> query = _dbContext.FinancialTransactions.AsNoTracking();

    if (type.HasValue)
    {
      query = query.Where(transaction => transaction.Type == type.Value);
    }

    if (status.HasValue)
    {
      query = query.Where(transaction => transaction.Status == status.Value);
    }

    if (occurredWithin is not null)
    {
      query = query.Where(transaction =>
          transaction.OccurredAt >= occurredWithin.From &&
          transaction.OccurredAt <= occurredWithin.To);
    }

    return await query
        .OrderByDescending(transaction => transaction.OccurredAt)
        .ToArrayAsync(cancellationToken);
  }

  public async Task<IReadOnlyList<FinancialTransaction>> ListByReferenceAsync(
      ReferenceType referenceType,
      Guid referenceId,
      CancellationToken cancellationToken = default)
  {
    return await _dbContext.FinancialTransactions
        .Where(transaction =>
            transaction.ReferenceType == referenceType &&
            transaction.ReferenceId == referenceId)
        .OrderByDescending(transaction => transaction.OccurredAt)
        .ToArrayAsync(cancellationToken);
  }

  public Task AddAsync(FinancialTransaction transaction, CancellationToken cancellationToken = default)
  {
    return _dbContext.FinancialTransactions.AddAsync(transaction, cancellationToken).AsTask();
  }

  public Task UpdateAsync(FinancialTransaction transaction, CancellationToken cancellationToken = default)
  {
    AttachIfDetached(transaction);
    return Task.CompletedTask;
  }

  private void AttachIfDetached(FinancialTransaction transaction)
  {
    if (_dbContext.Entry(transaction).State == EntityState.Detached)
    {
      _dbContext.FinancialTransactions.Attach(transaction);
    }
  }
}
