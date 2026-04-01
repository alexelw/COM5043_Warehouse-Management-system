using Wms.Domain.Entities;
using Wms.Domain.Enums;
using Wms.Domain.ValueObjects;

namespace Wms.Domain.Repositories;

/// <summary>
/// Handles storage and lookup for stock movement audit records.
/// </summary>
public interface IStockMovementRepository
{
  Task<IReadOnlyList<StockMovement>> ListAsync(
      Guid? productId = null,
      StockMovementType? type = null,
      DateRange? occurredWithin = null,
      CancellationToken cancellationToken = default);

  Task<IReadOnlyList<StockMovement>> ListByProductAsync(
      Guid productId,
      DateRange? occurredWithin = null,
      CancellationToken cancellationToken = default);

  Task AddAsync(StockMovement stockMovement, CancellationToken cancellationToken = default);

  Task AddRangeAsync(IEnumerable<StockMovement> stockMovements, CancellationToken cancellationToken = default);
}
