using Wms.Application.Common.Models;
using Wms.Application.Orders;
using Wms.Application.Tests.Support;
using Wms.Domain.Entities;
using Wms.Domain.Enums;
using Wms.Domain.ValueObjects;

namespace Wms.Application.Tests;

public class OrderServiceTests
{
  [Fact]
  public async Task CreateCustomerOrderAsync_ConfirmsOrderReducesStockAndPostsSale()
  {
    var customerRepository = new InMemoryCustomerRepository();
    var customerOrderRepository = new InMemoryCustomerOrderRepository();
    var productRepository = new InMemoryProductRepository();
    var stockMovementRepository = new InMemoryStockMovementRepository();
    var transactionRepository = new InMemoryTransactionRepository();
    var unitOfWork = new TrackingUnitOfWork();
    var clock = new FakeClock();

    var supplierId = Guid.NewGuid();
    var product = new Product(supplierId, "SKU-100", "Retail Widget", 2, new Money(6m), 10);
    await productRepository.AddAsync(product);

    var service = new OrderService(
        customerRepository,
        customerOrderRepository,
        productRepository,
        stockMovementRepository,
        transactionRepository,
        unitOfWork,
        clock);

    var result = await service.CreateCustomerOrderAsync(new CreateCustomerOrderRequest(
        new CustomerInputModel("Jane Customer", "jane@example.com", "07123456789"),
        new[]
        {
                new CustomerOrderLineInput(product.ProductId, 3, new MoneyModel(8m, "GBP")),
        }));

    var updatedProduct = await productRepository.GetByIdAsync(product.ProductId);
    var transactions = await transactionRepository.ListByReferenceAsync(
        ReferenceType.CustomerOrder,
        result.CustomerOrderId);

    Assert.Equal(CustomerOrderStatus.Confirmed, result.Status);
    Assert.NotNull(updatedProduct);
    Assert.Equal(7, updatedProduct!.QuantityOnHand);

    var transaction = Assert.Single(transactions);
    Assert.Equal(FinancialTransactionStatus.Posted, transaction.Status);
    Assert.Equal(FinancialTransactionType.Sale, transaction.Type);
    Assert.Equal(24m, transaction.Amount.Amount);
    Assert.Equal(1, unitOfWork.SaveChangesCalls);
  }

  [Fact]
  public async Task CancelCustomerOrderAsync_WhenConfirmed_RestoresStockAndCreatesReversal()
  {
    var customerRepository = new InMemoryCustomerRepository();
    var customerOrderRepository = new InMemoryCustomerOrderRepository();
    var productRepository = new InMemoryProductRepository();
    var stockMovementRepository = new InMemoryStockMovementRepository();
    var transactionRepository = new InMemoryTransactionRepository();
    var unitOfWork = new TrackingUnitOfWork();
    var clock = new FakeClock();

    var supplierId = Guid.NewGuid();
    var product = new Product(supplierId, "SKU-101", "Retail Widget", 2, new Money(6m), 10);
    await productRepository.AddAsync(product);

    var service = new OrderService(
        customerRepository,
        customerOrderRepository,
        productRepository,
        stockMovementRepository,
        transactionRepository,
        unitOfWork,
        clock);

    var order = await service.CreateCustomerOrderAsync(new CreateCustomerOrderRequest(
        new CustomerInputModel("Jane Customer", "jane@example.com", "07123456789"),
        [new CustomerOrderLineInput(product.ProductId, 3, new MoneyModel(8m, "GBP"))]));

    var result = await service.CancelCustomerOrderAsync(
        order.CustomerOrderId,
        new CancelCustomerOrderRequest("Customer changed their mind"));

    var updatedProduct = await productRepository.GetByIdAsync(product.ProductId);
    var transactions = await transactionRepository.ListByReferenceAsync(
        ReferenceType.CustomerOrder,
        order.CustomerOrderId);

    Assert.Equal(CustomerOrderStatus.Cancelled, result.Status);
    Assert.NotNull(updatedProduct);
    Assert.Equal(10, updatedProduct!.QuantityOnHand);
    Assert.Equal(2, stockMovementRepository.Items.Count);
    Assert.Contains(transactions, transaction =>
        transaction.Status == FinancialTransactionStatus.Reversed &&
        !transaction.IsReversal);
    Assert.Contains(transactions, transaction =>
        transaction.Status == FinancialTransactionStatus.Posted &&
        transaction.IsReversal &&
        transaction.ReversalOfTransactionId.HasValue);
    Assert.Equal(2, unitOfWork.SaveChangesCalls);
  }
}
