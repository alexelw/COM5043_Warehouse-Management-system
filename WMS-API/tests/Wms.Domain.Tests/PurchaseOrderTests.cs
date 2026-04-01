using Wms.Domain.Entities;
using Wms.Domain.Enums;
using Wms.Domain.Exceptions;
using Wms.Domain.ValueObjects;

namespace Wms.Domain.Tests;

public class PurchaseOrderTests
{
  [Fact]
  public void Constructor_WhenNoLinesProvided_ThrowsDomainRuleViolationException()
  {
    var action = () => new PurchaseOrder(Guid.NewGuid(), Array.Empty<PurchaseOrderLine>());

    Assert.Throws<DomainRuleViolationException>(action);
  }

  [Fact]
  public void Constructor_AssignsPurchaseOrderIdToLines()
  {
    var line = new PurchaseOrderLine(Guid.NewGuid(), 5, new Money(4m));
    var purchaseOrder = new PurchaseOrder(Guid.NewGuid(), new[] { line });

    Assert.Equal(purchaseOrder.PurchaseOrderId, line.PurchaseOrderId);
  }

  [Fact]
  public void Constructor_WhenDuplicateProductLinesUseSameUnitCost_MergesThemIntoSingleLine()
  {
    var productId = Guid.NewGuid();
    var purchaseOrder = new PurchaseOrder(
        Guid.NewGuid(),
        new[]
        {
            new PurchaseOrderLine(productId, 2, new Money(4m)),
            new PurchaseOrderLine(productId, 3, new Money(4m)),
        });

    var line = Assert.Single(purchaseOrder.Lines);
    Assert.Equal(5, line.QuantityOrdered);
    Assert.Equal(20m, purchaseOrder.TotalOrderedAmount.Amount);
  }

  [Fact]
  public void Constructor_WhenDuplicateProductLinesUseDifferentUnitCosts_ThrowsDomainRuleViolationException()
  {
    var productId = Guid.NewGuid();
    var action = () => new PurchaseOrder(
        Guid.NewGuid(),
        new[]
        {
            new PurchaseOrderLine(productId, 2, new Money(4m)),
            new PurchaseOrderLine(productId, 3, new Money(5m)),
        });

    Assert.Throws<DomainRuleViolationException>(action);
  }

  [Fact]
  public void ReceiveGoods_WhenReceiptIsPartial_SetsStatusToPartiallyReceived()
  {
    var productId = Guid.NewGuid();
    var purchaseOrder = new PurchaseOrder(
        Guid.NewGuid(),
        new[] { new PurchaseOrderLine(productId, 10, new Money(4m)) });
    var receipt = new GoodsReceipt(
        purchaseOrder.PurchaseOrderId,
        new[] { new GoodsReceiptLine(productId, 4) });

    purchaseOrder.ReceiveGoods(receipt);

    Assert.Equal(PurchaseOrderStatus.PartiallyReceived, purchaseOrder.Status);
    Assert.Equal(6, purchaseOrder.GetOutstandingQuantity(productId));
    Assert.Contains(receipt, purchaseOrder.Receipts);
  }

  [Fact]
  public void ReceiveGoods_WhenReceiptCompletesOrder_SetsStatusToCompleted()
  {
    var productId = Guid.NewGuid();
    var purchaseOrder = new PurchaseOrder(
        Guid.NewGuid(),
        new[] { new PurchaseOrderLine(productId, 10, new Money(4m)) });
    var receipt = new GoodsReceipt(
        purchaseOrder.PurchaseOrderId,
        new[] { new GoodsReceiptLine(productId, 10) });

    purchaseOrder.ReceiveGoods(receipt);

    Assert.Equal(PurchaseOrderStatus.Completed, purchaseOrder.Status);
    Assert.Equal(0, purchaseOrder.GetOutstandingQuantity(productId));
  }

  [Fact]
  public void ReceiveGoods_WhenReceiptQuantityExceedsOrdered_ThrowsDomainRuleViolationException()
  {
    var productId = Guid.NewGuid();
    var purchaseOrder = new PurchaseOrder(
        Guid.NewGuid(),
        new[] { new PurchaseOrderLine(productId, 10, new Money(4m)) });
    var receipt = new GoodsReceipt(
        purchaseOrder.PurchaseOrderId,
        new[] { new GoodsReceiptLine(productId, 11) });

    var action = () => purchaseOrder.ReceiveGoods(receipt);

    Assert.Throws<DomainRuleViolationException>(action);
  }

  [Fact]
  public void Cancel_WhenOrderIsCompleted_ThrowsInvalidStatusTransitionException()
  {
    var productId = Guid.NewGuid();
    var purchaseOrder = new PurchaseOrder(
        Guid.NewGuid(),
        new[] { new PurchaseOrderLine(productId, 10, new Money(4m)) });
    var receipt = new GoodsReceipt(
        purchaseOrder.PurchaseOrderId,
        new[] { new GoodsReceiptLine(productId, 10) });

    purchaseOrder.ReceiveGoods(receipt);

    var action = () => purchaseOrder.Cancel();

    Assert.Throws<InvalidStatusTransitionException>(action);
  }
}
