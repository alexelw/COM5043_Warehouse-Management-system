using Wms.Application.Abstractions;
using Wms.Application.Common.Exceptions;
using Wms.Application.Common.Mappers;
using Wms.Application.PurchaseOrders;
using Wms.Domain.Entities;
using Wms.Domain.Enums;
using Wms.Domain.Repositories;

namespace Wms.Application.Suppliers;

public sealed class SupplierService : ISupplierService
{
  private readonly ISupplierRepository _supplierRepository;
  private readonly IPurchaseOrderRepository _purchaseOrderRepository;
  private readonly IUnitOfWork _unitOfWork;

  public SupplierService(
      ISupplierRepository supplierRepository,
      IPurchaseOrderRepository purchaseOrderRepository,
      IUnitOfWork unitOfWork)
  {
    this._supplierRepository = supplierRepository;
    this._purchaseOrderRepository = purchaseOrderRepository;
    this._unitOfWork = unitOfWork;
  }

  public async Task<SupplierResult> CreateSupplierAsync(SupplierWriteModel model, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(model);

    var supplier = new Supplier(
        model.Name,
        ApplicationMapping.ToRequiredContactDetails(model.Email, model.Phone, model.Address));

    await this._supplierRepository.AddAsync(supplier, cancellationToken);
    await this._unitOfWork.SaveChangesAsync(cancellationToken);
    return supplier.ToResult();
  }

  public async Task<IReadOnlyList<SupplierResult>> GetSuppliersAsync(string? searchTerm = null, CancellationToken cancellationToken = default)
  {
    var suppliers = await this._supplierRepository.ListAsync(searchTerm, cancellationToken);
    return suppliers.Select(supplier => supplier.ToResult()).ToArray();
  }

  public async Task<SupplierResult> GetSupplierAsync(Guid supplierId, CancellationToken cancellationToken = default)
  {
    var supplier = await GetSupplierEntityAsync(supplierId, cancellationToken);
    return supplier.ToResult();
  }

  public async Task<SupplierResult> UpdateSupplierAsync(Guid supplierId, SupplierWriteModel model, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(model);

    var supplier = await GetSupplierEntityAsync(supplierId, cancellationToken);
    supplier.UpdateDetails(
        model.Name,
        ApplicationMapping.ToRequiredContactDetails(model.Email, model.Phone, model.Address));

    await this._supplierRepository.UpdateAsync(supplier, cancellationToken);
    await this._unitOfWork.SaveChangesAsync(cancellationToken);
    return supplier.ToResult();
  }

  public async Task DeleteSupplierAsync(Guid supplierId, CancellationToken cancellationToken = default)
  {
    _ = await GetSupplierEntityAsync(supplierId, cancellationToken);

    var hasOpenOrders = await this._purchaseOrderRepository.HasOpenOrdersForSupplierAsync(supplierId, cancellationToken);
    if (hasOpenOrders)
    {
      throw new ConflictException("Cannot delete supplier with open purchase orders.");
    }

    await this._supplierRepository.DeleteAsync(supplierId, cancellationToken);
    await this._unitOfWork.SaveChangesAsync(cancellationToken);
  }

  public async Task<IReadOnlyList<PurchaseOrderResult>> GetSupplierPurchaseOrdersAsync(
      Guid supplierId,
      PurchaseOrderStatus? status = null,
      DateTime? from = null,
      DateTime? to = null,
      CancellationToken cancellationToken = default)
  {
    _ = await GetSupplierEntityAsync(supplierId, cancellationToken);

    var purchaseOrders = await this._purchaseOrderRepository.ListBySupplierAsync(
        supplierId,
        status,
        ApplicationMapping.ToDateRange(from, to),
        cancellationToken);

    return purchaseOrders.Select(purchaseOrder => purchaseOrder.ToResult()).ToArray();
  }

  private async Task<Supplier> GetSupplierEntityAsync(Guid supplierId, CancellationToken cancellationToken)
  {
    var supplier = await this._supplierRepository.GetByIdAsync(supplierId, cancellationToken);
    if (supplier is null)
    {
      throw new NotFoundException(nameof(Supplier), supplierId);
    }

    return supplier;
  }
}
