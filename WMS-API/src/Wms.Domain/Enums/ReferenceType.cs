namespace Wms.Domain.Enums;

/// <summary>
/// Identifies the business record a stock or finance event points back to.
/// </summary>
public enum ReferenceType
{
  PurchaseOrder = 1,
  CustomerOrder = 2,
  GoodsReceipt = 3,
  StockAdjustment = 4,
}
