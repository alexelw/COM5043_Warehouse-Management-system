using Wms.Domain.Entities;
using Wms.Domain.Exceptions;

namespace Wms.Domain.Tests;

public class GoodsReceiptTests
{
  [Fact]
  public void Constructor_WhenNoLinesProvided_ThrowsDomainRuleViolationException()
  {
    var action = () => new GoodsReceipt(Guid.NewGuid(), Array.Empty<GoodsReceiptLine>());

    Assert.Throws<DomainRuleViolationException>(action);
  }

  [Fact]
  public void Constructor_AssignsGoodsReceiptIdToLines()
  {
    var line = new GoodsReceiptLine(Guid.NewGuid(), 5);
    var receipt = new GoodsReceipt(Guid.NewGuid(), new[] { line });

    Assert.Equal(receipt.GoodsReceiptId, line.GoodsReceiptId);
  }

  [Fact]
  public void Constructor_WhenDuplicateProductLinesProvided_MergesThemIntoSingleLine()
  {
    var productId = Guid.NewGuid();
    var receipt = new GoodsReceipt(
        Guid.NewGuid(),
        new[]
        {
            new GoodsReceiptLine(productId, 2),
            new GoodsReceiptLine(productId, 3),
        });

    var line = Assert.Single(receipt.Lines);
    Assert.Equal(5, line.QuantityReceived);
  }
}
