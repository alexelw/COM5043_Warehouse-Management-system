using Wms.Contracts.Common;

namespace Wms.Contracts.Inventory;

public sealed record UpdateProductRequest : IValidatableObject
{
  [Required]
  public string Sku { get; init; } = string.Empty;

  [Required]
  public Guid SupplierId { get; init; }

  [Range(0, int.MaxValue)]
  public int ReorderThreshold { get; init; }

  [Required]
  public string Name { get; init; } = string.Empty;

  [Required]
  public MoneyDto UnitCost { get; init; } = new();

  public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
  {
    if (this.SupplierId == Guid.Empty)
    {
      yield return new ValidationResult(
          "SupplierId is required.",
          new[] { nameof(this.SupplierId) });
    }

    if (this.UnitCost.Amount <= 0)
    {
      yield return new ValidationResult(
          "Unit cost amount must be greater than 0.",
          new[] { $"{nameof(this.UnitCost)}.{nameof(this.UnitCost.Amount)}" });
    }
  }
}
