using Wms.Domain.Exceptions;

namespace Wms.Domain.Entities;

public class GoodsReceipt : LineCollectionDocument<GoodsReceiptLine>
{
  private readonly List<GoodsReceiptLine> _lines = new();

  private GoodsReceipt()
  {
  }

  public GoodsReceipt(Guid purchaseOrderId, IEnumerable<GoodsReceiptLine> lines, DateTime? receivedAt = null)
  {
    if (purchaseOrderId == Guid.Empty)
    {
      throw new DomainRuleViolationException("Purchase order id is required.");
    }

    this.GoodsReceiptId = Guid.NewGuid();
    this.PurchaseOrderId = purchaseOrderId;
    this.ReceivedAt = receivedAt ?? DateTime.UtcNow;

    this.AddLines(NormalizeLines(lines));
    this.EnsureHasLines("Goods receipt must contain at least one line.");
  }

  protected override List<GoodsReceiptLine> MutableLines => this._lines;

  public Guid GoodsReceiptId { get; private set; }

  public Guid PurchaseOrderId { get; private set; }

  public DateTime ReceivedAt { get; private set; }

  protected override void PrepareLineForAdd(GoodsReceiptLine line)
  {
    line.AssignToGoodsReceipt(this.GoodsReceiptId);
  }

  private static IEnumerable<GoodsReceiptLine> NormalizeLines(IEnumerable<GoodsReceiptLine> lines)
  {
    ArgumentNullException.ThrowIfNull(lines);

    return NormalizeGroupedLines(lines);
  }

  private static IEnumerable<GoodsReceiptLine> NormalizeGroupedLines(IEnumerable<GoodsReceiptLine> lines)
  {
    foreach (var lineGroup in lines.GroupBy(line => line.ProductId))
    {
      var normalizedLine = lineGroup.First();
      if (lineGroup.Count() == 1)
      {
        yield return normalizedLine;
        continue;
      }

      yield return new GoodsReceiptLine(
          lineGroup.Key,
          lineGroup.Sum(line => line.QuantityReceived));
    }
  }
}
