using Wms.Contracts.Common;

namespace Wms.Contracts.PurchaseOrders;

public sealed record PurchaseOrderLineRequest : IValidatableObject
{
  [Required]
  public Guid ProductId { get; init; }

  [Range(1, int.MaxValue)]
  public int Quantity { get; init; }

  [Required]
  public MoneyDto UnitCost { get; init; } = new();

  public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
  {
    if (this.ProductId == Guid.Empty)
    {
      yield return new ValidationResult(
          "ProductId is required.",
          new[] { nameof(this.ProductId) });
    }

    if (this.UnitCost.Amount <= 0)
    {
      yield return new ValidationResult(
          "Unit cost amount must be greater than 0.",
          new[] { $"{nameof(this.UnitCost)}.{nameof(this.UnitCost.Amount)}" });
    }
  }
}
