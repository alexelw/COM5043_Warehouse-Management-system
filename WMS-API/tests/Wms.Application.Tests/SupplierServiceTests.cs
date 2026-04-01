using Wms.Application.Common.Exceptions;
using Wms.Application.PurchaseOrders;
using Wms.Application.Suppliers;
using Wms.Application.Tests.Support;
using Wms.Domain.Entities;
using Wms.Domain.Enums;
using Wms.Domain.ValueObjects;

namespace Wms.Application.Tests;

public class SupplierServiceTests
{
  [Fact]
  public async Task CreateSupplierAsync_PersistsSupplierAndSavesChanges()
  {
    var supplierRepository = new InMemorySupplierRepository();
    var purchaseOrderRepository = new InMemoryPurchaseOrderRepository();
    var unitOfWork = new TrackingUnitOfWork();
    var service = new SupplierService(supplierRepository, purchaseOrderRepository, unitOfWork);

    var result = await service.CreateSupplierAsync(new SupplierWriteModel(
        "Acme Supplies",
        "orders@acme.test",
        "01234567890",
        "Unit 1"));

    var storedSupplier = await supplierRepository.GetByIdAsync(result.SupplierId);

    Assert.NotNull(storedSupplier);
    Assert.Equal("Acme Supplies", storedSupplier!.Name);
    Assert.Equal("orders@acme.test", storedSupplier.Contact.Email);
    Assert.Equal(1, unitOfWork.SaveChangesCalls);
  }

  [Fact]
  public async Task DeleteSupplierAsync_WhenOpenPurchaseOrdersExist_ThrowsConflictException()
  {
    var supplierRepository = new InMemorySupplierRepository();
    var purchaseOrderRepository = new InMemoryPurchaseOrderRepository();
    var unitOfWork = new TrackingUnitOfWork();
    var service = new SupplierService(supplierRepository, purchaseOrderRepository, unitOfWork);

    var supplier = new Supplier("Acme Supplies", new ContactDetails("orders@acme.test", null, null));
    var purchaseOrder = new PurchaseOrder(
        supplier.SupplierId,
        [new PurchaseOrderLine(Guid.NewGuid(), 2, new Money(5m))]);

    await supplierRepository.AddAsync(supplier);
    await purchaseOrderRepository.AddAsync(purchaseOrder);

    var exception = await Assert.ThrowsAsync<ConflictException>(() =>
        service.DeleteSupplierAsync(supplier.SupplierId));

    Assert.Equal("Cannot delete supplier with open purchase orders.", exception.Message);
    Assert.NotNull(await supplierRepository.GetByIdAsync(supplier.SupplierId));
    Assert.Equal(0, unitOfWork.SaveChangesCalls);
  }

  [Fact]
  public async Task GetSupplierPurchaseOrdersAsync_ReturnsOrdersForSelectedSupplier()
  {
    var supplierRepository = new InMemorySupplierRepository();
    var purchaseOrderRepository = new InMemoryPurchaseOrderRepository();
    var unitOfWork = new TrackingUnitOfWork();
    var service = new SupplierService(supplierRepository, purchaseOrderRepository, unitOfWork);

    var supplier = new Supplier("Acme Supplies", new ContactDetails("orders@acme.test", null, null));
    var otherSupplier = new Supplier("Northwind", new ContactDetails("hello@northwind.test", null, null));
    await supplierRepository.AddAsync(supplier);
    await supplierRepository.AddAsync(otherSupplier);

    await purchaseOrderRepository.AddAsync(new PurchaseOrder(
        supplier.SupplierId,
        [new PurchaseOrderLine(Guid.NewGuid(), 3, new Money(7m))]));
    await purchaseOrderRepository.AddAsync(new PurchaseOrder(
        otherSupplier.SupplierId,
        [new PurchaseOrderLine(Guid.NewGuid(), 1, new Money(9m))]));

    var results = await service.GetSupplierPurchaseOrdersAsync(
        supplier.SupplierId,
        PurchaseOrderStatus.Pending);

    var order = Assert.Single(results);
    Assert.Equal(supplier.SupplierId, order.SupplierId);
    Assert.Equal(PurchaseOrderStatus.Pending, order.Status);
  }
}
