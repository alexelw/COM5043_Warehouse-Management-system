using Microsoft.EntityFrameworkCore;
using Wms.Domain.Entities;
using Wms.Domain.Enums;
using Wms.Domain.Repositories;
using Wms.Domain.ValueObjects;

namespace Wms.Infrastructure.Persistence.Repositories;

public sealed class StockMovementRepository : IStockMovementRepository
{
  private readonly WmsDbContext _dbContext;

  public StockMovementRepository(WmsDbContext dbContext)
  {
    _dbContext = dbContext;
  }

  public async Task<IReadOnlyList<StockMovement>> ListAsync(
      Guid? productId = null,
      StockMovementType? type = null,
      DateRange? occurredWithin = null,
      CancellationToken cancellationToken = default)
  {
    IQueryable<StockMovement> query = _dbContext.StockMovements.AsNoTracking();

    if (productId.HasValue)
    {
      query = query.Where(stockMovement => stockMovement.ProductId == productId.Value);
    }

    if (type.HasValue)
    {
      query = query.Where(stockMovement => stockMovement.Type == type.Value);
    }

    if (occurredWithin is not null)
    {
      query = query.Where(stockMovement =>
          stockMovement.OccurredAt >= occurredWithin.From &&
          stockMovement.OccurredAt <= occurredWithin.To);
    }

    return await query
        .OrderByDescending(stockMovement => stockMovement.OccurredAt)
        .ToArrayAsync(cancellationToken);
  }

  public Task<IReadOnlyList<StockMovement>> ListByProductAsync(
      Guid productId,
      DateRange? occurredWithin = null,
      CancellationToken cancellationToken = default)
  {
    return ListAsync(productId, null, occurredWithin, cancellationToken);
  }

  public Task AddAsync(StockMovement stockMovement, CancellationToken cancellationToken = default)
  {
    return _dbContext.StockMovements.AddAsync(stockMovement, cancellationToken).AsTask();
  }

  public Task AddRangeAsync(IEnumerable<StockMovement> stockMovements, CancellationToken cancellationToken = default)
  {
    _dbContext.StockMovements.AddRange(stockMovements);
    return Task.CompletedTask;
  }
}
