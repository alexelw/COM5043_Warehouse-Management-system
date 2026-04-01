using Wms.Domain.Entities;
using Wms.Domain.ValueObjects;

namespace Wms.Domain.Repositories;

/// <summary>
/// Handles storage and lookup for goods receipts.
/// </summary>
public interface IGoodsReceiptRepository
{
  Task<GoodsReceipt?> GetByIdAsync(Guid goodsReceiptId, CancellationToken cancellationToken = default);

  Task<IReadOnlyList<GoodsReceipt>> ListByPurchaseOrderAsync(
      Guid purchaseOrderId,
      DateRange? receivedWithin = null,
      CancellationToken cancellationToken = default);

  Task AddAsync(GoodsReceipt goodsReceipt, CancellationToken cancellationToken = default);
}
