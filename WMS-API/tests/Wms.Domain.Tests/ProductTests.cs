using Wms.Domain.Entities;
using Wms.Domain.Exceptions;
using Wms.Domain.ValueObjects;

namespace Wms.Domain.Tests;

public class ProductTests
{
  [Fact]
  public void DecreaseStock_WhenQuantityExceedsAvailable_ThrowsInsufficientStockException()
  {
    var product = new Product(Guid.NewGuid(), "SKU-001", "Widget", 5, new Money(10m), 3);

    var action = () => product.DecreaseStock(4);

    Assert.Throws<InsufficientStockException>(action);
  }

  [Fact]
  public void SetReorderLevel_WhenNegative_ThrowsDomainRuleViolationException()
  {
    var product = new Product(Guid.NewGuid(), "SKU-001", "Widget", 5, new Money(10m), 3);

    var action = () => product.SetReorderLevel(-1);

    Assert.Throws<DomainRuleViolationException>(action);
  }

  [Fact]
  public void HasSufficientStock_WhenQuantityWithinAvailable_ReturnsTrue()
  {
    var product = new Product(Guid.NewGuid(), "SKU-001", "Widget", 5, new Money(10m), 8);

    var result = product.HasSufficientStock(5);

    Assert.True(result);
  }

  [Fact]
  public void AdjustStock_WhenAdjustmentWouldReduceBelowZero_ThrowsInsufficientStockException()
  {
    var product = new Product(Guid.NewGuid(), "SKU-001", "Widget", 5, new Money(10m), 3);

    var action = () => product.AdjustStock(-4);

    Assert.Throws<InsufficientStockException>(action);
  }
}
