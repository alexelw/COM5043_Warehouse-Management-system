using Wms.Domain.Entities;
using Wms.Domain.Exceptions;
using Wms.Domain.ValueObjects;

namespace Wms.Domain.Tests;

public class LineEntityTests
{
  [Fact]
  public void PurchaseOrderLine_WhenQuantityIsZero_ThrowsDomainRuleViolationException()
  {
    var action = () => new PurchaseOrderLine(Guid.NewGuid(), 0, new Money(3m));

    Assert.Throws<DomainRuleViolationException>(action);
  }

  [Fact]
  public void CustomerOrderLine_WhenQuantityIsZero_ThrowsDomainRuleViolationException()
  {
    var action = () => new CustomerOrderLine(Guid.NewGuid(), 0, new Money(5m));

    Assert.Throws<DomainRuleViolationException>(action);
  }

  [Fact]
  public void GoodsReceiptLine_WhenQuantityIsZero_ThrowsDomainRuleViolationException()
  {
    var action = () => new GoodsReceiptLine(Guid.NewGuid(), 0);

    Assert.Throws<DomainRuleViolationException>(action);
  }

  [Fact]
  public void PurchaseOrderLine_LineTotal_ReturnsUnitCostMultipliedByQuantity()
  {
    var line = new PurchaseOrderLine(Guid.NewGuid(), 3, new Money(4.50m));

    Assert.Equal(13.50m, line.LineTotal.Amount);
  }
}
