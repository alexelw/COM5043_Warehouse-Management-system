using Wms.Domain.Exceptions;
using Wms.Domain.ValueObjects;

namespace Wms.Domain.Entities;

public class CustomerOrderLine
{
  private CustomerOrderLine()
  {
  }

  public CustomerOrderLine(Guid productId, int quantity, Money unitPriceAtSale)
  {
    this.CustomerOrderLineId = Guid.NewGuid();
    this.ChangeProduct(productId);
    this.SetQuantity(quantity);
    this.ChangeUnitPrice(unitPriceAtSale);
  }

  public Guid CustomerOrderLineId { get; private set; }

  public Guid CustomerOrderId { get; private set; }

  public Guid ProductId { get; private set; }

  public int Quantity { get; private set; }

  public Money UnitPriceAtSale { get; private set; } = Money.Zero;

  public Money LineTotal => this.UnitPriceAtSale * this.Quantity;

  internal void AssignToCustomerOrder(Guid customerOrderId)
  {
    if (customerOrderId == Guid.Empty)
    {
      throw new DomainRuleViolationException("Customer order id is required.");
    }

    this.CustomerOrderId = customerOrderId;
  }

  private void ChangeProduct(Guid productId)
  {
    if (productId == Guid.Empty)
    {
      throw new DomainRuleViolationException("Product id is required.");
    }

    this.ProductId = productId;
  }

  private void SetQuantity(int quantity)
  {
    if (quantity <= 0)
    {
      throw new DomainRuleViolationException("Customer order line quantity must be greater than zero.");
    }

    this.Quantity = quantity;
  }

  private void ChangeUnitPrice(Money unitPriceAtSale)
  {
    ArgumentNullException.ThrowIfNull(unitPriceAtSale);
    this.UnitPriceAtSale = unitPriceAtSale;
  }
}
