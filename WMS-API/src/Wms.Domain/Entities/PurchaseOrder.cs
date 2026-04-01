using Wms.Domain.Enums;
using Wms.Domain.Exceptions;
using Wms.Domain.ValueObjects;

namespace Wms.Domain.Entities;

public class PurchaseOrder : LineCollectionDocument<PurchaseOrderLine>
{
  private readonly List<PurchaseOrderLine> _lines = new();
  private readonly List<GoodsReceipt> _receipts = new();

  private PurchaseOrder()
  {
  }

  public PurchaseOrder(Guid supplierId, IEnumerable<PurchaseOrderLine> lines, DateTime? createdAt = null)
  {
    if (supplierId == Guid.Empty)
    {
      throw new DomainRuleViolationException("Supplier id is required.");
    }

    this.PurchaseOrderId = Guid.NewGuid();
    this.SupplierId = supplierId;
    this.Status = PurchaseOrderStatus.Pending;
    this.CreatedAt = createdAt ?? DateTime.UtcNow;

    this.AddLines(NormalizeLines(lines));
    this.EnsureHasLines("Purchase order must contain at least one line.");
  }

  protected override List<PurchaseOrderLine> MutableLines => this._lines;

  public Guid PurchaseOrderId { get; private set; }

  public Guid SupplierId { get; private set; }

  public PurchaseOrderStatus Status { get; private set; }

  public DateTime CreatedAt { get; private set; }

  public IReadOnlyCollection<GoodsReceipt> Receipts => this._receipts.AsReadOnly();

  public Money TotalOrderedAmount => this.Lines.Aggregate(Money.Zero, static (total, line) => total + line.LineTotal);

  public void AddLine(Guid productId, int quantityOrdered, Money unitCostAtOrder)
  {
    EnsureStatus(PurchaseOrderStatus.Pending);
    base.AddLine(new PurchaseOrderLine(productId, quantityOrdered, unitCostAtOrder));
  }

  public void Cancel()
  {
    if (this.Status is not PurchaseOrderStatus.Pending and not PurchaseOrderStatus.PartiallyReceived)
    {
      throw new InvalidStatusTransitionException(
          nameof(PurchaseOrder),
          this.Status.ToString(),
          PurchaseOrderStatus.Cancelled.ToString());
    }

    this.Status = PurchaseOrderStatus.Cancelled;
  }

  public void ReceiveGoods(GoodsReceipt receipt)
  {
    ArgumentNullException.ThrowIfNull(receipt);

    if (receipt.PurchaseOrderId != this.PurchaseOrderId)
    {
      throw new DomainRuleViolationException("Goods receipt does not belong to this purchase order.");
    }

    if (this.Status is not PurchaseOrderStatus.Pending and not PurchaseOrderStatus.PartiallyReceived)
    {
      throw new InvalidStatusTransitionException(
          nameof(PurchaseOrder),
          this.Status.ToString(),
          "ReceiveGoods");
    }

    ValidateReceipt(receipt);
    this._receipts.Add(receipt);
    this.Status = HasOutstandingQuantities()
        ? PurchaseOrderStatus.PartiallyReceived
        : PurchaseOrderStatus.Completed;
  }

  public int GetOutstandingQuantity(Guid productId)
  {
    if (productId == Guid.Empty)
    {
      throw new DomainRuleViolationException("Product id is required.");
    }

    var ordered = this._lines.Where(line => line.ProductId == productId).Sum(line => line.QuantityOrdered);
    var received = GetReceivedQuantity(productId);
    return Math.Max(0, ordered - received);
  }

  private void EnsureStatus(PurchaseOrderStatus expectedStatus)
  {
    if (this.Status != expectedStatus)
    {
      throw new InvalidStatusTransitionException(
          nameof(PurchaseOrder),
          this.Status.ToString(),
          expectedStatus.ToString());
    }
  }

  private void ValidateReceipt(GoodsReceipt receipt)
  {
    var receiptQuantities = receipt.Lines
        .GroupBy(line => line.ProductId)
        .ToDictionary(group => group.Key, group => group.Sum(line => line.QuantityReceived));

    foreach (var (productId, receiptQuantity) in receiptQuantities)
    {
      var orderedQuantity = this._lines
          .Where(line => line.ProductId == productId)
          .Sum(line => line.QuantityOrdered);

      if (orderedQuantity == 0)
      {
        throw new DomainRuleViolationException("Received product is not part of the purchase order.");
      }

      var totalReceivedAfterReceipt = GetReceivedQuantity(productId) + receiptQuantity;
      if (totalReceivedAfterReceipt > orderedQuantity)
      {
        throw new DomainRuleViolationException("Received quantity cannot exceed ordered quantity.");
      }
    }
  }

  private int GetReceivedQuantity(Guid productId)
  {
    return this._receipts
        .SelectMany(receipt => receipt.Lines)
        .Where(line => line.ProductId == productId)
        .Sum(line => line.QuantityReceived);
  }

  private bool HasOutstandingQuantities()
  {
    return this._lines
        .GroupBy(line => line.ProductId)
        .Any(group => GetReceivedQuantity(group.Key) < group.Sum(line => line.QuantityOrdered));
  }

  protected override void PrepareLineForAdd(PurchaseOrderLine line)
  {
    line.AssignToPurchaseOrder(this.PurchaseOrderId);
  }

  private static IEnumerable<PurchaseOrderLine> NormalizeLines(IEnumerable<PurchaseOrderLine> lines)
  {
    ArgumentNullException.ThrowIfNull(lines);

    foreach (var lineGroup in lines.GroupBy(line => line.ProductId))
    {
      var normalizedLine = lineGroup.First();
      if (lineGroup.Count() == 1)
      {
        yield return normalizedLine;
        continue;
      }

      var hasMixedUnitCosts = lineGroup.Any(line =>
          line.UnitCostAtOrder.Amount != normalizedLine.UnitCostAtOrder.Amount ||
          !string.Equals(line.UnitCostAtOrder.Currency, normalizedLine.UnitCostAtOrder.Currency, StringComparison.Ordinal));

      if (hasMixedUnitCosts)
      {
        throw new DomainRuleViolationException("Duplicate product lines must use the same unit cost.");
      }

      yield return new PurchaseOrderLine(
          lineGroup.Key,
          lineGroup.Sum(line => line.QuantityOrdered),
          normalizedLine.UnitCostAtOrder);
    }
  }
}
