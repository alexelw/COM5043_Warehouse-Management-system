using Wms.Domain.Entities;
using Wms.Domain.Enums;
using Wms.Domain.Exceptions;

namespace Wms.Domain.Tests;

public class StockMovementTests
{
  [Fact]
  public void Constructor_WhenReceiptQuantityIsZero_ThrowsDomainRuleViolationException()
  {
    var action = () => new StockMovement(
        Guid.NewGuid(),
        StockMovementType.Receipt,
        0,
        ReferenceType.GoodsReceipt,
        Guid.NewGuid());

    Assert.Throws<DomainRuleViolationException>(action);
  }

  [Fact]
  public void Constructor_WhenAdjustmentReasonIsMissing_ThrowsDomainRuleViolationException()
  {
    var action = () => new StockMovement(
        Guid.NewGuid(),
        StockMovementType.Adjustment,
        -2,
        ReferenceType.StockAdjustment,
        Guid.NewGuid(),
        reason: " ");

    Assert.Throws<DomainRuleViolationException>(action);
  }

  [Fact]
  public void CreateAdjustment_WhenQuantityIsNegative_CreatesAdjustmentMovement()
  {
    var movement = StockMovement.CreateAdjustment(Guid.NewGuid(), -2, "Stock count correction", Guid.NewGuid());

    Assert.Equal(StockMovementType.Adjustment, movement.Type);
    Assert.Equal(ReferenceType.StockAdjustment, movement.ReferenceType);
    Assert.Equal(-2, movement.Quantity);
    Assert.Equal("Stock count correction", movement.Reason);
  }
}
