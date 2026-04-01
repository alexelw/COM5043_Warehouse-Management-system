using Wms.Domain.Enums;
using Wms.Domain.Exceptions;

namespace Wms.Domain.Entities;

public class StockMovement
{
  private StockMovement()
  {
  }

  public StockMovement(
      Guid productId,
      StockMovementType type,
      int quantity,
      ReferenceType referenceType,
      Guid referenceId,
      DateTime? occurredAt = null,
      string? reason = null)
  {
    if (productId == Guid.Empty)
    {
      throw new DomainRuleViolationException("Product id is required.");
    }

    if (referenceId == Guid.Empty)
    {
      throw new DomainRuleViolationException("Reference id is required.");
    }

    if (!Enum.IsDefined(type))
    {
      throw new DomainRuleViolationException("Stock movement type is invalid.");
    }

    if (!Enum.IsDefined(referenceType))
    {
      throw new DomainRuleViolationException("Reference type is invalid.");
    }

    ValidateQuantity(type, quantity);

    var normalizedReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
    if (type == StockMovementType.Adjustment && normalizedReason is null)
    {
      throw new DomainRuleViolationException("Stock adjustments must include a reason.");
    }

    this.StockMovementId = Guid.NewGuid();
    this.ProductId = productId;
    this.Type = type;
    this.Quantity = quantity;
    this.ReferenceType = referenceType;
    this.ReferenceId = referenceId;
    this.OccurredAt = occurredAt ?? DateTime.UtcNow;
    this.Reason = normalizedReason;
  }

  public Guid StockMovementId { get; private set; }

  public Guid ProductId { get; private set; }

  public StockMovementType Type { get; private set; }

  public int Quantity { get; private set; }

  public DateTime OccurredAt { get; private set; }

  public string? Reason { get; private set; }

  public ReferenceType ReferenceType { get; private set; }

  public Guid ReferenceId { get; private set; }

  public static StockMovement CreateReceipt(
      Guid productId,
      int quantity,
      ReferenceType referenceType,
      Guid referenceId,
      DateTime? occurredAt = null)
  {
    return new StockMovement(productId, StockMovementType.Receipt, quantity, referenceType, referenceId, occurredAt);
  }

  public static StockMovement CreateIssue(
      Guid productId,
      int quantity,
      ReferenceType referenceType,
      Guid referenceId,
      DateTime? occurredAt = null)
  {
    return new StockMovement(productId, StockMovementType.Issue, quantity, referenceType, referenceId, occurredAt);
  }

  public static StockMovement CreateAdjustment(
      Guid productId,
      int quantity,
      string reason,
      Guid referenceId,
      DateTime? occurredAt = null)
  {
    return new StockMovement(
        productId,
        StockMovementType.Adjustment,
        quantity,
        ReferenceType.StockAdjustment,
        referenceId,
        occurredAt,
        reason);
  }

  private static void ValidateQuantity(StockMovementType type, int quantity)
  {
    if (type == StockMovementType.Adjustment)
    {
      if (quantity == 0)
      {
        throw new DomainRuleViolationException("Adjustment quantity must be non-zero.");
      }

      return;
    }

    if (quantity <= 0)
    {
      throw new DomainRuleViolationException("Receipt and issue quantities must be greater than zero.");
    }
  }
}
