using Wms.Domain.Exceptions;
using Wms.Domain.ValueObjects;

namespace Wms.Domain.Entities;

public class PurchaseOrderLine
{
  private PurchaseOrderLine()
  {
  }

  public PurchaseOrderLine(Guid productId, int quantityOrdered, Money unitCostAtOrder)
  {
    this.PurchaseOrderLineId = Guid.NewGuid();
    this.ChangeProduct(productId);
    this.SetQuantityOrdered(quantityOrdered);
    this.ChangeUnitCostAtOrder(unitCostAtOrder);
  }

  public Guid PurchaseOrderLineId { get; private set; }

  public Guid PurchaseOrderId { get; private set; }

  public Guid ProductId { get; private set; }

  public int QuantityOrdered { get; private set; }

  public Money UnitCostAtOrder { get; private set; } = Money.Zero;

  public Money LineTotal => this.UnitCostAtOrder * this.QuantityOrdered;

  internal void AssignToPurchaseOrder(Guid purchaseOrderId)
  {
    if (purchaseOrderId == Guid.Empty)
    {
      throw new DomainRuleViolationException("Purchase order id is required.");
    }

    this.PurchaseOrderId = purchaseOrderId;
  }

  private void ChangeProduct(Guid productId)
  {
    if (productId == Guid.Empty)
    {
      throw new DomainRuleViolationException("Product id is required.");
    }

    this.ProductId = productId;
  }

  private void SetQuantityOrdered(int quantityOrdered)
  {
    if (quantityOrdered <= 0)
    {
      throw new DomainRuleViolationException("Purchase order line quantity must be greater than zero.");
    }

    this.QuantityOrdered = quantityOrdered;
  }

  private void ChangeUnitCostAtOrder(Money unitCostAtOrder)
  {
    ArgumentNullException.ThrowIfNull(unitCostAtOrder);
    this.UnitCostAtOrder = unitCostAtOrder;
  }
}
