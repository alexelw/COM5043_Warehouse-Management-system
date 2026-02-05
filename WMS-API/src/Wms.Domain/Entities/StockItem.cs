namespace Wms.Domain.Entities;

public class StockItem
{
    private StockItem()
    {
    }

    public StockItem(string sku, string name, int reorderLevel)
    {
        if (string.IsNullOrWhiteSpace(sku))
        {
            throw new ArgumentException("SKU is required.", nameof(sku));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name is required.", nameof(name));
        }

        if (reorderLevel < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(reorderLevel), "Reorder level cannot be negative.");
        }

        this.Id = Guid.NewGuid();
        this.Sku = sku.Trim();
        this.Name = name.Trim();
        this.ReorderLevel = reorderLevel;
    }

    public Guid Id { get; private set; }

    public string Sku { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public int QuantityOnHand { get; private set; }

    public int ReorderLevel { get; private set; }

    public bool IsLowStock => this.QuantityOnHand <= this.ReorderLevel;

    public void Receive(int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Received quantity must be greater than zero.");
        }

        this.QuantityOnHand += quantity;
    }

    public void Allocate(int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Allocated quantity must be greater than zero.");
        }

        if (quantity > this.QuantityOnHand)
        {
            throw new InvalidOperationException("Cannot allocate more stock than is available.");
        }

        this.QuantityOnHand -= quantity;
    }
}
