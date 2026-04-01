using Wms.Domain.Exceptions;

namespace Wms.Domain.Entities;

public class GoodsReceiptLine
{
  private GoodsReceiptLine()
  {
  }

  public GoodsReceiptLine(Guid productId, int quantityReceived)
  {
    this.GoodsReceiptLineId = Guid.NewGuid();
    this.ChangeProduct(productId);
    this.SetQuantityReceived(quantityReceived);
  }

  public Guid GoodsReceiptLineId { get; private set; }

  public Guid GoodsReceiptId { get; private set; }

  public Guid ProductId { get; private set; }

  public int QuantityReceived { get; private set; }

  internal void AssignToGoodsReceipt(Guid goodsReceiptId)
  {
    if (goodsReceiptId == Guid.Empty)
    {
      throw new DomainRuleViolationException("Goods receipt id is required.");
    }

    this.GoodsReceiptId = goodsReceiptId;
  }

  private void ChangeProduct(Guid productId)
  {
    if (productId == Guid.Empty)
    {
      throw new DomainRuleViolationException("Product id is required.");
    }

    this.ProductId = productId;
  }

  private void SetQuantityReceived(int quantityReceived)
  {
    if (quantityReceived <= 0)
    {
      throw new DomainRuleViolationException("Received quantity must be greater than zero.");
    }

    this.QuantityReceived = quantityReceived;
  }
}
