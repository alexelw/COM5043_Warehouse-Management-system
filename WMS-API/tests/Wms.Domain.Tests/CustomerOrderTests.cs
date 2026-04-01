using Wms.Domain.Entities;
using Wms.Domain.Enums;
using Wms.Domain.Exceptions;
using Wms.Domain.ValueObjects;

namespace Wms.Domain.Tests;

public class CustomerOrderTests
{
  [Fact]
  public void Confirm_WhenAvailableStockIsInsufficient_ThrowsInsufficientStockException()
  {
    var product = new Product(Guid.NewGuid(), "SKU-001", "Widget", 2, new Money(8m), quantityOnHand: 4);
    var customerOrder = new CustomerOrder(
        Guid.NewGuid(),
        new[]
        {
                new CustomerOrderLine(product.ProductId, 5, new Money(12m)),
        });

    var products = new[] { product };

    var action = () => customerOrder.Confirm(products);

    Assert.Throws<InsufficientStockException>(action);
  }

  [Fact]
  public void Confirm_WhenAvailableStockIsSufficient_SetsStatusToConfirmed()
  {
    var product = new Product(Guid.NewGuid(), "SKU-001", "Widget", 2, new Money(8m), quantityOnHand: 5);
    var customerOrder = new CustomerOrder(
        Guid.NewGuid(),
        new[]
        {
                new CustomerOrderLine(product.ProductId, 5, new Money(12m)),
        });

    customerOrder.Confirm(new[] { product });

    Assert.Equal(CustomerOrderStatus.Confirmed, customerOrder.Status);
  }

  [Fact]
  public void Confirm_WhenProductStockDetailsAreMissing_ThrowsDomainRuleViolationException()
  {
    var customerOrder = new CustomerOrder(
        Guid.NewGuid(),
        new[]
        {
                new CustomerOrderLine(Guid.NewGuid(), 5, new Money(12m)),
        });

    var action = () => customerOrder.Confirm(Array.Empty<Product>());

    Assert.Throws<DomainRuleViolationException>(action);
  }

  [Fact]
  public void Cancel_WhenOrderIsAlreadyCancelled_ThrowsInvalidStatusTransitionException()
  {
    var product = new Product(Guid.NewGuid(), "SKU-001", "Widget", 2, new Money(8m), quantityOnHand: 5);
    var customerOrder = new CustomerOrder(
        Guid.NewGuid(),
        new[]
        {
                new CustomerOrderLine(product.ProductId, 5, new Money(12m)),
        });

    customerOrder.Cancel();

    var action = () => customerOrder.Cancel();

    Assert.Throws<InvalidStatusTransitionException>(action);
  }
}
