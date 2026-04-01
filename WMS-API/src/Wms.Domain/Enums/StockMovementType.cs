namespace Wms.Domain.Enums;

/// <summary>
/// Describes why stock changed for audit purposes.
/// </summary>
public enum StockMovementType
{
  Receipt = 1,
  Issue = 2,
  Adjustment = 3,
}
