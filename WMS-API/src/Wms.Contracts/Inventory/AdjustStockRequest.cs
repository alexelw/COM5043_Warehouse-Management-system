namespace Wms.Contracts.Inventory;

public sealed record AdjustStockRequest : IValidatableObject
{
  public int Quantity { get; init; }

  [Required]
  public string Reason { get; init; } = string.Empty;

  public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
  {
    if (this.Quantity == 0)
    {
      yield return new ValidationResult(
          "Quantity must be a non-zero integer.",
          new[] { nameof(this.Quantity) });
    }
  }
}
