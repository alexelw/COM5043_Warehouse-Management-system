using Wms.Application.Common.Exceptions;
using Wms.Application.Common.Models;
using Wms.Application.Inventory;
using Wms.Application.Tests.Support;
using Wms.Domain.Entities;
using Wms.Domain.Enums;
using Wms.Domain.ValueObjects;

namespace Wms.Application.Tests;

public class InventoryServiceTests
{
  [Fact]
  public async Task CreateProductAsync_WhenSkuAlreadyExists_ThrowsConflictException()
  {
    var supplierRepository = new InMemorySupplierRepository();
    var productRepository = new InMemoryProductRepository();
    var stockMovementRepository = new InMemoryStockMovementRepository();
    var unitOfWork = new TrackingUnitOfWork();
    var clock = new FakeClock();
    var service = CreateService(productRepository, supplierRepository, stockMovementRepository, unitOfWork, clock);

    var supplier = new Supplier("Acme Supplies", new ContactDetails("orders@acme.test", null, null));
    await supplierRepository.AddAsync(supplier);
    await productRepository.AddAsync(new Product(supplier.SupplierId, "SKU-001", "Widget", 3, new Money(10m)));

    var exception = await Assert.ThrowsAsync<ConflictException>(() => service.CreateProductAsync(
        new ProductWriteModel(
            "SKU-001",
            "New Widget",
            supplier.SupplierId,
            5,
            new MoneyModel(12m, "GBP"))));

    Assert.Equal("Product SKU 'SKU-001' already exists.", exception.Message);
    Assert.Equal(0, unitOfWork.SaveChangesCalls);
  }

  [Fact]
  public async Task AdjustStockAsync_CreatesAdjustmentMovementAndReturnsUpdatedLevel()
  {
    var supplierRepository = new InMemorySupplierRepository();
    var productRepository = new InMemoryProductRepository();
    var stockMovementRepository = new InMemoryStockMovementRepository();
    var unitOfWork = new TrackingUnitOfWork();
    var clock = new FakeClock();
    var service = CreateService(productRepository, supplierRepository, stockMovementRepository, unitOfWork, clock);

    var product = new Product(Guid.NewGuid(), "SKU-200", "Counted Widget", 2, new Money(4m), 5);
    await productRepository.AddAsync(product);

    var result = await service.AdjustStockAsync(
        product.ProductId,
        new AdjustStockRequest(3, "Cycle count correction"));

    var updatedProduct = await productRepository.GetByIdAsync(product.ProductId);
    var movement = Assert.Single(stockMovementRepository.Items);

    Assert.NotNull(updatedProduct);
    Assert.Equal(8, updatedProduct!.QuantityOnHand);
    Assert.Equal(8, result.QuantityOnHand);
    Assert.Equal(StockMovementType.Adjustment, movement.Type);
    Assert.Equal(ReferenceType.StockAdjustment, movement.ReferenceType);
    Assert.Equal("Cycle count correction", movement.Reason);
    Assert.Equal(1, unitOfWork.SaveChangesCalls);
  }

  [Fact]
  public async Task DeleteProductAsync_WhenProductIsInUse_ThrowsConflictException()
  {
    var supplierRepository = new InMemorySupplierRepository();
    var productRepository = new InMemoryProductRepository();
    var stockMovementRepository = new InMemoryStockMovementRepository();
    var unitOfWork = new TrackingUnitOfWork();
    var clock = new FakeClock();
    var service = CreateService(productRepository, supplierRepository, stockMovementRepository, unitOfWork, clock);

    var product = new Product(Guid.NewGuid(), "SKU-300", "Protected Widget", 1, new Money(2m));
    await productRepository.AddAsync(product);
    productRepository.InUseProductIds.Add(product.ProductId);

    var exception = await Assert.ThrowsAsync<ConflictException>(() => service.DeleteProductAsync(product.ProductId));

    Assert.Equal("Cannot delete product because it is referenced by existing warehouse activity.", exception.Message);
    Assert.NotNull(await productRepository.GetByIdAsync(product.ProductId));
    Assert.Equal(0, unitOfWork.SaveChangesCalls);
  }

  private static InventoryService CreateService(
      InMemoryProductRepository productRepository,
      InMemorySupplierRepository supplierRepository,
      InMemoryStockMovementRepository stockMovementRepository,
      TrackingUnitOfWork unitOfWork,
      FakeClock clock)
  {
    return new InventoryService(
        productRepository,
        supplierRepository,
        stockMovementRepository,
        unitOfWork,
        clock);
  }
}
