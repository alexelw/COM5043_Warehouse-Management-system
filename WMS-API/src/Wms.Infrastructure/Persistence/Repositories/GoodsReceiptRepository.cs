using Microsoft.EntityFrameworkCore;
using Wms.Domain.Entities;
using Wms.Domain.Repositories;
using Wms.Domain.ValueObjects;

namespace Wms.Infrastructure.Persistence.Repositories;

public sealed class GoodsReceiptRepository : IGoodsReceiptRepository
{
  private readonly WmsDbContext _dbContext;

  public GoodsReceiptRepository(WmsDbContext dbContext)
  {
    _dbContext = dbContext;
  }

  public Task<GoodsReceipt?> GetByIdAsync(Guid goodsReceiptId, CancellationToken cancellationToken = default)
  {
    return _dbContext.GoodsReceipts
        .Include(receipt => receipt.Lines)
        .SingleOrDefaultAsync(receipt => receipt.GoodsReceiptId == goodsReceiptId, cancellationToken);
  }

  public async Task<IReadOnlyList<GoodsReceipt>> ListByPurchaseOrderAsync(
      Guid purchaseOrderId,
      DateRange? receivedWithin = null,
      CancellationToken cancellationToken = default)
  {
    IQueryable<GoodsReceipt> query = _dbContext.GoodsReceipts
        .AsNoTracking()
        .Include(receipt => receipt.Lines)
        .Where(receipt => receipt.PurchaseOrderId == purchaseOrderId);

    if (receivedWithin is not null)
    {
      query = query.Where(receipt =>
          receipt.ReceivedAt >= receivedWithin.From &&
          receipt.ReceivedAt <= receivedWithin.To);
    }

    return await query
        .OrderByDescending(receipt => receipt.ReceivedAt)
        .ToArrayAsync(cancellationToken);
  }

  public Task AddAsync(GoodsReceipt goodsReceipt, CancellationToken cancellationToken = default)
  {
    return _dbContext.GoodsReceipts.AddAsync(goodsReceipt, cancellationToken).AsTask();
  }
}
