using Wms.Application.Common.Models;
using Wms.Application.PurchaseOrders;
using Wms.Application.Tests.Support;
using Wms.Domain.Entities;
using Wms.Domain.Enums;
using Wms.Domain.ValueObjects;

namespace Wms.Application.Tests;

public class PurchaseOrderServiceTests
{
  [Fact]
  public async Task CreatePurchaseOrderAsync_CreatesPendingExpenseForTotal()
  {
    var supplierRepository = new InMemorySupplierRepository();
    var productRepository = new InMemoryProductRepository();
    var purchaseOrderRepository = new InMemoryPurchaseOrderRepository();
    var goodsReceiptRepository = new InMemoryGoodsReceiptRepository();
    var stockMovementRepository = new InMemoryStockMovementRepository();
    var transactionRepository = new InMemoryTransactionRepository();
    var unitOfWork = new TrackingUnitOfWork();
    var clock = new FakeClock();

    var supplier = new Supplier("Acme Supplies", new ContactDetails("supplier@acme.test", null, null));
    var product = new Product(supplier.SupplierId, "SKU-001", "Widget", 5, new Money(10m));
    await supplierRepository.AddAsync(supplier);
    await productRepository.AddAsync(product);

    var service = new PurchaseOrderService(
        purchaseOrderRepository,
        supplierRepository,
        productRepository,
        goodsReceiptRepository,
        stockMovementRepository,
        transactionRepository,
        unitOfWork,
        clock);

    var result = await service.CreatePurchaseOrderAsync(new CreatePurchaseOrderRequest(
        supplier.SupplierId,
        new[]
        {
                new PurchaseOrderLineInput(product.ProductId, 3, new MoneyModel(12.50m, "GBP")),
        }));

    var transactions = await transactionRepository.ListByReferenceAsync(
        ReferenceType.PurchaseOrder,
        result.PurchaseOrderId);

    var transaction = Assert.Single(transactions);
    Assert.Equal(FinancialTransactionStatus.Pending, transaction.Status);
    Assert.Equal(FinancialTransactionType.PurchaseExpense, transaction.Type);
    Assert.Equal(37.50m, transaction.Amount.Amount);
    Assert.Equal(1, unitOfWork.SaveChangesCalls);
  }

  [Fact]
  public async Task ReceiveDeliveryAsync_ForPartialReceipt_PostsReceivedExpenseAndRefreshesPendingBalance()
  {
    var supplierRepository = new InMemorySupplierRepository();
    var productRepository = new InMemoryProductRepository();
    var purchaseOrderRepository = new InMemoryPurchaseOrderRepository();
    var goodsReceiptRepository = new InMemoryGoodsReceiptRepository();
    var stockMovementRepository = new InMemoryStockMovementRepository();
    var transactionRepository = new InMemoryTransactionRepository();
    var unitOfWork = new TrackingUnitOfWork();
    var clock = new FakeClock();

    var supplier = new Supplier("Acme Supplies", new ContactDetails("supplier@acme.test", null, null));
    var product = new Product(supplier.SupplierId, "SKU-001", "Widget", 5, new Money(10m));
    await supplierRepository.AddAsync(supplier);
    await productRepository.AddAsync(product);

    var service = new PurchaseOrderService(
        purchaseOrderRepository,
        supplierRepository,
        productRepository,
        goodsReceiptRepository,
        stockMovementRepository,
        transactionRepository,
        unitOfWork,
        clock);

    var purchaseOrder = await service.CreatePurchaseOrderAsync(new CreatePurchaseOrderRequest(
        supplier.SupplierId,
        new[]
        {
                new PurchaseOrderLineInput(product.ProductId, 10, new MoneyModel(5m, "GBP")),
        }));

    await service.ReceiveDeliveryAsync(
        purchaseOrder.PurchaseOrderId,
        new ReceiveDeliveryRequest(new[]
        {
                new GoodsReceiptLineInput(product.ProductId, 4),
        }));

    var updatedProduct = await productRepository.GetByIdAsync(product.ProductId);
    var transactions = await transactionRepository.ListByReferenceAsync(
        ReferenceType.PurchaseOrder,
        purchaseOrder.PurchaseOrderId);

    Assert.NotNull(updatedProduct);
    Assert.Equal(4, updatedProduct!.QuantityOnHand);
    Assert.Contains(transactions, transaction =>
        transaction.Status == FinancialTransactionStatus.Posted &&
        transaction.Amount.Amount == 20m);
    Assert.Contains(transactions, transaction =>
        transaction.Status == FinancialTransactionStatus.Pending &&
        transaction.Amount.Amount == 30m);
    Assert.Contains(transactions, transaction =>
        transaction.Status == FinancialTransactionStatus.Voided &&
        transaction.Amount.Amount == 50m);
  }

  [Fact]
  public async Task CancelPurchaseOrderAsync_VoidsPendingExpenseAndMarksOrderCancelled()
  {
    var supplierRepository = new InMemorySupplierRepository();
    var productRepository = new InMemoryProductRepository();
    var purchaseOrderRepository = new InMemoryPurchaseOrderRepository();
    var goodsReceiptRepository = new InMemoryGoodsReceiptRepository();
    var stockMovementRepository = new InMemoryStockMovementRepository();
    var transactionRepository = new InMemoryTransactionRepository();
    var unitOfWork = new TrackingUnitOfWork();
    var clock = new FakeClock();

    var supplier = new Supplier("Acme Supplies", new ContactDetails("supplier@acme.test", null, null));
    var product = new Product(supplier.SupplierId, "SKU-002", "Widget", 5, new Money(10m));
    await supplierRepository.AddAsync(supplier);
    await productRepository.AddAsync(product);

    var service = new PurchaseOrderService(
        purchaseOrderRepository,
        supplierRepository,
        productRepository,
        goodsReceiptRepository,
        stockMovementRepository,
        transactionRepository,
        unitOfWork,
        clock);

    var purchaseOrder = await service.CreatePurchaseOrderAsync(new CreatePurchaseOrderRequest(
        supplier.SupplierId,
        [new PurchaseOrderLineInput(product.ProductId, 3, new MoneyModel(12.50m, "GBP"))]));

    var result = await service.CancelPurchaseOrderAsync(
        purchaseOrder.PurchaseOrderId,
        new CancelPurchaseOrderRequest("Supplier issue"));

    var transactions = await transactionRepository.ListByReferenceAsync(
        ReferenceType.PurchaseOrder,
        purchaseOrder.PurchaseOrderId);

    var transaction = Assert.Single(transactions);
    Assert.Equal(PurchaseOrderStatus.Cancelled, result.Status);
    Assert.Equal(FinancialTransactionStatus.Voided, transaction.Status);
    Assert.Equal(2, unitOfWork.SaveChangesCalls);
  }

  [Fact]
  public async Task ReceiveDeliveryAsync_ForCompleteReceipt_DoesNotLeavePendingExpense()
  {
    var supplierRepository = new InMemorySupplierRepository();
    var productRepository = new InMemoryProductRepository();
    var purchaseOrderRepository = new InMemoryPurchaseOrderRepository();
    var goodsReceiptRepository = new InMemoryGoodsReceiptRepository();
    var stockMovementRepository = new InMemoryStockMovementRepository();
    var transactionRepository = new InMemoryTransactionRepository();
    var unitOfWork = new TrackingUnitOfWork();
    var clock = new FakeClock();

    var supplier = new Supplier("Acme Supplies", new ContactDetails("supplier@acme.test", null, null));
    var product = new Product(supplier.SupplierId, "SKU-003", "Widget", 5, new Money(10m));
    await supplierRepository.AddAsync(supplier);
    await productRepository.AddAsync(product);

    var service = new PurchaseOrderService(
        purchaseOrderRepository,
        supplierRepository,
        productRepository,
        goodsReceiptRepository,
        stockMovementRepository,
        transactionRepository,
        unitOfWork,
        clock);

    var purchaseOrder = await service.CreatePurchaseOrderAsync(new CreatePurchaseOrderRequest(
        supplier.SupplierId,
        [new PurchaseOrderLineInput(product.ProductId, 4, new MoneyModel(5m, "GBP"))]));

    await service.ReceiveDeliveryAsync(
        purchaseOrder.PurchaseOrderId,
        new ReceiveDeliveryRequest([new GoodsReceiptLineInput(product.ProductId, 4)]));

    var updatedOrder = await service.GetPurchaseOrderAsync(purchaseOrder.PurchaseOrderId);
    var transactions = await transactionRepository.ListByReferenceAsync(
        ReferenceType.PurchaseOrder,
        purchaseOrder.PurchaseOrderId);

    Assert.Equal(PurchaseOrderStatus.Completed, updatedOrder.Status);
    Assert.DoesNotContain(transactions, transaction => transaction.Status == FinancialTransactionStatus.Pending);
    Assert.Contains(transactions, transaction =>
        transaction.Status == FinancialTransactionStatus.Posted &&
        transaction.Amount.Amount == 20m);
    Assert.Contains(transactions, transaction =>
        transaction.Status == FinancialTransactionStatus.Voided &&
        transaction.Amount.Amount == 20m);
  }
}
