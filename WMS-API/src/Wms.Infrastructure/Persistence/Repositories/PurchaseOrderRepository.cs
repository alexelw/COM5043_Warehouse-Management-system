using Microsoft.EntityFrameworkCore;
using Wms.Domain.Entities;
using Wms.Domain.Enums;
using Wms.Domain.Repositories;
using Wms.Domain.ValueObjects;

namespace Wms.Infrastructure.Persistence.Repositories;

public sealed class PurchaseOrderRepository : IPurchaseOrderRepository
{
  private readonly WmsDbContext _dbContext;

  public PurchaseOrderRepository(WmsDbContext dbContext)
  {
    _dbContext = dbContext;
  }

  public Task<PurchaseOrder?> GetByIdAsync(Guid purchaseOrderId, CancellationToken cancellationToken = default)
  {
    return _dbContext.PurchaseOrders
        .Include(purchaseOrder => purchaseOrder.Lines)
        .Include(purchaseOrder => purchaseOrder.Receipts)
        .ThenInclude(receipt => receipt.Lines)
        .AsSplitQuery()
        .SingleOrDefaultAsync(
            purchaseOrder => purchaseOrder.PurchaseOrderId == purchaseOrderId,
            cancellationToken);
  }

  public async Task<IReadOnlyList<PurchaseOrder>> ListAsync(
      Guid? supplierId = null,
      PurchaseOrderStatus? status = null,
      DateRange? createdWithin = null,
      CancellationToken cancellationToken = default)
  {
    IQueryable<PurchaseOrder> query = _dbContext.PurchaseOrders
        .AsNoTracking()
        .Include(purchaseOrder => purchaseOrder.Lines);

    if (supplierId.HasValue)
    {
      query = query.Where(purchaseOrder => purchaseOrder.SupplierId == supplierId.Value);
    }

    if (status.HasValue)
    {
      query = query.Where(purchaseOrder => purchaseOrder.Status == status.Value);
    }

    if (createdWithin is not null)
    {
      query = query.Where(purchaseOrder =>
          purchaseOrder.CreatedAt >= createdWithin.From &&
          purchaseOrder.CreatedAt <= createdWithin.To);
    }

    return await query
        .OrderByDescending(purchaseOrder => purchaseOrder.CreatedAt)
        .ToArrayAsync(cancellationToken);
  }

  public Task<IReadOnlyList<PurchaseOrder>> ListBySupplierAsync(
      Guid supplierId,
      PurchaseOrderStatus? status = null,
      DateRange? createdWithin = null,
      CancellationToken cancellationToken = default)
  {
    return ListAsync(supplierId, status, createdWithin, cancellationToken);
  }

  public Task<bool> HasOpenOrdersForSupplierAsync(Guid supplierId, CancellationToken cancellationToken = default)
  {
    return _dbContext.PurchaseOrders.AnyAsync(
        purchaseOrder =>
            purchaseOrder.SupplierId == supplierId &&
            (purchaseOrder.Status == PurchaseOrderStatus.Pending ||
             purchaseOrder.Status == PurchaseOrderStatus.PartiallyReceived),
        cancellationToken);
  }

  public Task AddAsync(PurchaseOrder purchaseOrder, CancellationToken cancellationToken = default)
  {
    return _dbContext.PurchaseOrders.AddAsync(purchaseOrder, cancellationToken).AsTask();
  }

  public Task UpdateAsync(PurchaseOrder purchaseOrder, CancellationToken cancellationToken = default)
  {
    AttachIfDetached(purchaseOrder);
    return Task.CompletedTask;
  }

  private void AttachIfDetached(PurchaseOrder purchaseOrder)
  {
    if (_dbContext.Entry(purchaseOrder).State == EntityState.Detached)
    {
      _dbContext.PurchaseOrders.Attach(purchaseOrder);
    }
  }
}
