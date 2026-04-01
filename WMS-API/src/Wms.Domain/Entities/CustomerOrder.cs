using Wms.Domain.Enums;
using Wms.Domain.Exceptions;
using Wms.Domain.ValueObjects;

namespace Wms.Domain.Entities;

public class CustomerOrder : LineCollectionDocument<CustomerOrderLine>
{
  private readonly List<CustomerOrderLine> _lines = new();

  private CustomerOrder()
  {
  }

  public CustomerOrder(Guid customerId, IEnumerable<CustomerOrderLine> lines, DateTime? createdAt = null)
  {
    if (customerId == Guid.Empty)
    {
      throw new DomainRuleViolationException("Customer id is required.");
    }

    this.CustomerOrderId = Guid.NewGuid();
    this.CustomerId = customerId;
    this.Status = CustomerOrderStatus.Draft;
    this.CreatedAt = createdAt ?? DateTime.UtcNow;

    this.AddLines(lines);
    this.EnsureHasLines("Customer order must contain at least one line.");
  }

  protected override List<CustomerOrderLine> MutableLines => this._lines;

  public Guid CustomerOrderId { get; private set; }

  public Guid CustomerId { get; private set; }

  public CustomerOrderStatus Status { get; private set; }

  public DateTime CreatedAt { get; private set; }

  public Money TotalAmount => this.Lines.Aggregate(Money.Zero, static (total, line) => total + line.LineTotal);

  public void AddLine(Guid productId, int quantity, Money unitPrice)
  {
    EnsureStatus(CustomerOrderStatus.Draft, "AddLine");
    base.AddLine(new CustomerOrderLine(productId, quantity, unitPrice));
  }

  public void Confirm(IEnumerable<Product> products)
  {
    ArgumentNullException.ThrowIfNull(products);

    this.EnsureHasLines("Customer order must contain at least one line.");
    EnsureStatus(CustomerOrderStatus.Draft, CustomerOrderStatus.Confirmed.ToString());

    var productsById = products
        .GroupBy(product => product.ProductId)
        .ToDictionary(group => group.Key, group => group.First());

    foreach (var lineGroup in this._lines.GroupBy(line => line.ProductId))
    {
      var requiredQuantity = lineGroup.Sum(line => line.Quantity);
      if (!productsById.TryGetValue(lineGroup.Key, out var product))
      {
        throw new DomainRuleViolationException("Product stock details are required to confirm the order.");
      }

      if (!product.HasSufficientStock(requiredQuantity))
      {
        throw new InsufficientStockException(product.Sku, requiredQuantity, product.QuantityOnHand);
      }
    }

    this.Status = CustomerOrderStatus.Confirmed;
  }

  public void Cancel()
  {
    if (this.Status is not CustomerOrderStatus.Draft and not CustomerOrderStatus.Confirmed)
    {
      throw new InvalidStatusTransitionException(
          nameof(CustomerOrder),
          this.Status.ToString(),
          CustomerOrderStatus.Cancelled.ToString());
    }

    this.Status = CustomerOrderStatus.Cancelled;
  }

  private void EnsureStatus(CustomerOrderStatus expectedStatus, string targetAction)
  {
    if (this.Status != expectedStatus)
    {
      throw new InvalidStatusTransitionException(
          nameof(CustomerOrder),
          this.Status.ToString(),
          targetAction);
    }
  }

  protected override void PrepareLineForAdd(CustomerOrderLine line)
  {
    line.AssignToCustomerOrder(this.CustomerOrderId);
  }
}
