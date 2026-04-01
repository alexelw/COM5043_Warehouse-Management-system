using Wms.Application.PurchaseOrders;
using Wms.Domain.Enums;

namespace Wms.Application.Suppliers;

public interface ISupplierService
{
  Task<SupplierResult> CreateSupplierAsync(SupplierWriteModel model, CancellationToken cancellationToken = default);

  Task<IReadOnlyList<SupplierResult>> GetSuppliersAsync(string? searchTerm = null, CancellationToken cancellationToken = default);

  Task<SupplierResult> GetSupplierAsync(Guid supplierId, CancellationToken cancellationToken = default);

  Task<SupplierResult> UpdateSupplierAsync(Guid supplierId, SupplierWriteModel model, CancellationToken cancellationToken = default);

  Task DeleteSupplierAsync(Guid supplierId, CancellationToken cancellationToken = default);

  Task<IReadOnlyList<PurchaseOrderResult>> GetSupplierPurchaseOrdersAsync(
      Guid supplierId,
      PurchaseOrderStatus? status = null,
      DateTime? from = null,
      DateTime? to = null,
      CancellationToken cancellationToken = default);
}
