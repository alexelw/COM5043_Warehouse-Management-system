using Wms.Domain.Exceptions;
using Wms.Domain.ValueObjects;

namespace Wms.Domain.Entities;

public class Product
{
  private Product()
  {
  }

  public Product(
      Guid supplierId,
      string sku,
      string name,
      int reorderLevel,
      Money unitCost,
      int quantityOnHand = 0)
  {
    this.ProductId = Guid.NewGuid();
    this.ChangeSupplier(supplierId);
    this.ChangeSku(sku);
    this.ChangeName(name);
    this.SetReorderLevel(reorderLevel);
    this.ChangeUnitCost(unitCost);
    this.SetInitialQuantity(quantityOnHand);
  }

  public Guid ProductId { get; private set; }

  public Guid SupplierId { get; private set; }

  public string Sku { get; private set; } = string.Empty;

  public string Name { get; private set; } = string.Empty;

  public int ReorderLevel { get; private set; }

  public Money UnitCost { get; private set; } = Money.Zero;

  public int QuantityOnHand { get; private set; }

  public bool IsLowStock => this.QuantityOnHand <= this.ReorderLevel;

  public bool HasSufficientStock(int quantity)
  {
    if (quantity <= 0)
    {
      throw new DomainRuleViolationException("Requested stock quantity must be greater than zero.");
    }

    return this.QuantityOnHand >= quantity;
  }

  public void ChangeSupplier(Guid supplierId)
  {
    if (supplierId == Guid.Empty)
    {
      throw new DomainRuleViolationException("Supplier id is required.");
    }

    this.SupplierId = supplierId;
  }

  public void ChangeSku(string sku)
  {
    this.Sku = NormalizeRequired(sku, "SKU is required.");
  }

  public void ChangeName(string name)
  {
    this.Name = NormalizeRequired(name, "Product name is required.");
  }

  public void SetReorderLevel(int reorderLevel)
  {
    if (reorderLevel < 0)
    {
      throw new DomainRuleViolationException("Reorder level cannot be negative.");
    }

    this.ReorderLevel = reorderLevel;
  }

  public void ChangeUnitCost(Money unitCost)
  {
    ArgumentNullException.ThrowIfNull(unitCost);
    this.UnitCost = unitCost;
  }

  public void IncreaseStock(int quantity)
  {
    if (quantity <= 0)
    {
      throw new DomainRuleViolationException("Stock increase quantity must be greater than zero.");
    }

    this.QuantityOnHand += quantity;
  }

  public void DecreaseStock(int quantity)
  {
    if (quantity <= 0)
    {
      throw new DomainRuleViolationException("Stock decrease quantity must be greater than zero.");
    }

    if (!this.HasSufficientStock(quantity))
    {
      throw new InsufficientStockException(this.Sku, quantity, this.QuantityOnHand);
    }

    this.QuantityOnHand -= quantity;
  }

  public void AdjustStock(int quantityDelta)
  {
    if (quantityDelta == 0)
    {
      throw new DomainRuleViolationException("Stock adjustment quantity must be non-zero.");
    }

    if (quantityDelta > 0)
    {
      this.IncreaseStock(quantityDelta);
      return;
    }

    this.DecreaseStock(Math.Abs(quantityDelta));
  }

  private void SetInitialQuantity(int quantityOnHand)
  {
    if (quantityOnHand < 0)
    {
      throw new DomainRuleViolationException("Quantity on hand cannot be negative.");
    }

    this.QuantityOnHand = quantityOnHand;
  }

  private static string NormalizeRequired(string value, string message)
  {
    if (string.IsNullOrWhiteSpace(value))
    {
      throw new DomainRuleViolationException(message);
    }

    return value.Trim();
  }
}
