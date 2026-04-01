using Wms.Domain.Entities;
using Wms.Domain.Enums;
using Wms.Domain.ValueObjects;

namespace Wms.Domain.Repositories;

/// <summary>
/// Handles storage and lookup for purchase orders.
/// </summary>
public interface IPurchaseOrderRepository
{
  Task<PurchaseOrder?> GetByIdAsync(Guid purchaseOrderId, CancellationToken cancellationToken = default);

  Task<IReadOnlyList<PurchaseOrder>> ListAsync(
      Guid? supplierId = null,
      PurchaseOrderStatus? status = null,
      DateRange? createdWithin = null,
      CancellationToken cancellationToken = default);

  Task<IReadOnlyList<PurchaseOrder>> ListBySupplierAsync(
      Guid supplierId,
      PurchaseOrderStatus? status = null,
      DateRange? createdWithin = null,
      CancellationToken cancellationToken = default);

  Task<bool> HasOpenOrdersForSupplierAsync(Guid supplierId, CancellationToken cancellationToken = default);

  Task AddAsync(PurchaseOrder purchaseOrder, CancellationToken cancellationToken = default);

  Task UpdateAsync(PurchaseOrder purchaseOrder, CancellationToken cancellationToken = default);
}
