using Wms.Contracts.Common;

namespace Wms.Contracts.Orders;

public sealed record CustomerOrderLineRequest : IValidatableObject
{
  [Required]
  public Guid ProductId { get; init; }

  [Range(1, int.MaxValue)]
  public int Quantity { get; init; }

  [Required]
  public MoneyDto UnitPrice { get; init; } = new();

  public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
  {
    if (this.ProductId == Guid.Empty)
    {
      yield return new ValidationResult(
          "ProductId is required.",
          new[] { nameof(this.ProductId) });
    }

    if (this.UnitPrice.Amount <= 0)
    {
      yield return new ValidationResult(
          "Unit price amount must be greater than 0.",
          new[] { $"{nameof(this.UnitPrice)}.{nameof(this.UnitPrice.Amount)}" });
    }
  }
}
